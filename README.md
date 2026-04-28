# Game Leaderboard

A real-time game leaderboard demo: Go + Redis backend with a Unity3D frontend.

```
game-leaderboard/         ← this repo
├── main.go               ┐
├── handler/              │  Go + Gin HTTP server
├── service/              │
├── repository/           │
├── model/                ┘
├── Dockerfile
├── docker-compose.yml
└── client/               ← Unity3D frontend (Unity 6)
    └── Assets/Scripts/
        ├── PlayerPanel.cs
        ├── OpponentPanel.cs
        └── LeaderboardPanel.cs
```

---

## Backend — Quick Start

**Prerequisites:** Docker Desktop

```bash
docker-compose up --build
```

This starts two containers:
- **Redis 7** on port `6379`
- **Go server** on port `8080`

The API will be available at `http://localhost:8080`.

To stop and remove all data:

```bash
docker-compose down -v
```

---

## Frontend — Unity Client Setup

**Prerequisites:** Unity 6 (6000.x) with TextMeshPro Essential Resources imported

### 1. Open the project

Open `client/` as a Unity project.

### 2. Chinese font (one-time setup)

The UI uses Microsoft JhengHei for Chinese characters. This font ships with Windows but cannot be redistributed in the repo.

Copy it manually:

```
C:\Windows\Fonts\msjh.ttc  →  client/Assets/Fonts/msjh.ttc
```

Then in the Unity Editor run **Tools → Build Leaderboard UI** to regenerate the UI.

### 3. Import TMP Essential Resources (if text appears blank)

In Unity: **Window → TextMeshPro → Import TMP Essential Resources**

### 4. Start Play mode

Make sure `docker-compose up` is running first, then press **Play** in Unity.

---

## Architecture

```
Unity Client
     │  HTTP (UnityWebRequest)
     ▼
Gin HTTP Server  (:8080)
     │
     ▼
Redis Sorted Set
```

### Why Redis Sorted Set?

| Operation | Redis Command | Time Complexity |
|---|---|---|
| Submit / update score | `ZADD GT` | O(log N) |
| Get Top N | `ZREVRANGE` | O(log N + M) |
| Get player rank | `ZREVRANK` | O(log N) |
| Remove player | `ZREM` | O(log N) |

`ZADD GT` ensures only the player's highest score is kept — no extra read-modify-write needed.

---

## API Reference

### Submit Score

```bash
curl -X POST http://localhost:8080/api/v1/scores \
  -H "Content-Type: application/json" \
  -d '{"player_id": "alice", "score": 9500}'
```

```json
{"message": "score submitted", "player_id": "alice", "score": 9500}
```

> Only the highest score is retained. Submitting a lower score has no effect.

### Get Leaderboard (Top N)

```bash
curl "http://localhost:8080/api/v1/leaderboard?limit=5"
```

```json
{
  "leaderboard": [
    {"rank": 1, "player_id": "bob",   "score": 12000},
    {"rank": 2, "player_id": "alice", "score": 9500}
  ],
  "total": 2
}
```

### Get Player Rank

```bash
curl http://localhost:8080/api/v1/players/alice/rank
```

### Remove Player

```bash
curl -X DELETE http://localhost:8080/api/v1/players/alice
```

### Health Check

```bash
curl http://localhost:8080/health
```

---

## Running Tests

```bash
go test ./... -v
```

---

## Scalability Discussion

**Current design** handles tens of thousands of concurrent players on a single Redis instance.

**To support millions of players:**

1. **Redis Cluster** — shard the sorted set across nodes by score range or consistent hashing.
2. **Read replicas** — route `ZREVRANGE` / `ZREVRANK` reads to replicas, writes to primary.
3. **Top-N cache** — cache the Top 10/100 in memory (refreshed every few seconds) to cut Redis round-trips.
4. **Pagination** — replace `limit` with cursor-based pagination for leaderboards beyond 1000 entries.
5. **Horizontal app scaling** — the stateless Go service scales behind a load balancer; only Redis is stateful.
