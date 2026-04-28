package service

import (
	"context"
	"errors"
	"testing"

	"github.com/redis/go-redis/v9"
	"github.com/onexgear/game-leaderboard/repository"
)

// mockRepo implements LeaderboardRepository in memory for unit tests.
type mockRepo struct {
	data map[string]float64
}

func newMockRepo() repository.LeaderboardRepository {
	return &mockRepo{data: make(map[string]float64)}
}

func (m *mockRepo) SubmitScore(_ context.Context, playerID string, score float64) error {
	if cur, ok := m.data[playerID]; !ok || score > cur {
		m.data[playerID] = score
	}
	return nil
}

func (m *mockRepo) GetTopN(_ context.Context, n int64) ([]redis.Z, error) {
	// simple sort by score desc
	type kv struct {
		id    string
		score float64
	}
	sorted := make([]kv, 0, len(m.data))
	for id, s := range m.data {
		sorted = append(sorted, kv{id, s})
	}
	// bubble sort (small dataset in tests)
	for i := 0; i < len(sorted); i++ {
		for j := i + 1; j < len(sorted); j++ {
			if sorted[j].score > sorted[i].score {
				sorted[i], sorted[j] = sorted[j], sorted[i]
			}
		}
	}
	if n > int64(len(sorted)) {
		n = int64(len(sorted))
	}
	result := make([]redis.Z, n)
	for i := int64(0); i < n; i++ {
		result[i] = redis.Z{Score: sorted[i].score, Member: sorted[i].id}
	}
	return result, nil
}

func (m *mockRepo) GetPlayerRank(_ context.Context, playerID string) (int64, error) {
	if _, ok := m.data[playerID]; !ok {
		return -1, errors.New("not found")
	}
	rank := int64(0)
	for _, s := range m.data {
		if s > m.data[playerID] {
			rank++
		}
	}
	return rank, nil
}

func (m *mockRepo) GetPlayerScore(_ context.Context, playerID string) (float64, error) {
	s, ok := m.data[playerID]
	if !ok {
		return 0, errors.New("not found")
	}
	return s, nil
}

func (m *mockRepo) RemovePlayer(_ context.Context, playerID string) error {
	if _, ok := m.data[playerID]; !ok {
		return errors.New("not found")
	}
	delete(m.data, playerID)
	return nil
}

// --- Tests ---

func TestSubmitScore_KeepsHighest(t *testing.T) {
	svc := NewLeaderboardService(newMockRepo())
	ctx := context.Background()

	_ = svc.SubmitScore(ctx, "alice", 100)
	_ = svc.SubmitScore(ctx, "alice", 50) // lower, should be ignored

	player, err := svc.GetPlayerRank(ctx, "alice")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if player.Score != 100 {
		t.Errorf("expected score 100, got %v", player.Score)
	}
}

func TestSubmitScore_UpdatesIfHigher(t *testing.T) {
	svc := NewLeaderboardService(newMockRepo())
	ctx := context.Background()

	_ = svc.SubmitScore(ctx, "bob", 200)
	_ = svc.SubmitScore(ctx, "bob", 300) // higher, should update

	player, err := svc.GetPlayerRank(ctx, "bob")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if player.Score != 300 {
		t.Errorf("expected score 300, got %v", player.Score)
	}
}

func TestGetLeaderboard_OrderedByScore(t *testing.T) {
	svc := NewLeaderboardService(newMockRepo())
	ctx := context.Background()

	_ = svc.SubmitScore(ctx, "alice", 100)
	_ = svc.SubmitScore(ctx, "bob", 300)
	_ = svc.SubmitScore(ctx, "carol", 200)

	entries, err := svc.GetLeaderboard(ctx, 3)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if len(entries) != 3 {
		t.Fatalf("expected 3 entries, got %d", len(entries))
	}
	if entries[0].PlayerID != "bob" || entries[0].Rank != 1 {
		t.Errorf("expected bob at rank 1, got %+v", entries[0])
	}
	if entries[1].PlayerID != "carol" || entries[1].Rank != 2 {
		t.Errorf("expected carol at rank 2, got %+v", entries[1])
	}
}

func TestGetPlayerRank_NotFound(t *testing.T) {
	svc := NewLeaderboardService(newMockRepo())
	_, err := svc.GetPlayerRank(context.Background(), "ghost")
	if err == nil {
		t.Error("expected error for unknown player")
	}
}

func TestRemovePlayer(t *testing.T) {
	svc := NewLeaderboardService(newMockRepo())
	ctx := context.Background()

	_ = svc.SubmitScore(ctx, "dave", 500)
	if err := svc.RemovePlayer(ctx, "dave"); err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if err := svc.RemovePlayer(ctx, "dave"); err == nil {
		t.Error("expected error when removing already-removed player")
	}
}

func TestSubmitScore_NegativeScore(t *testing.T) {
	svc := NewLeaderboardService(newMockRepo())
	err := svc.SubmitScore(context.Background(), "eve", -10)
	if err == nil {
		t.Error("expected error for negative score")
	}
}
