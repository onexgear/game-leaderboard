package model

type Player struct {
	ID    string  `json:"id"`
	Score float64 `json:"score"`
	Rank  int64   `json:"rank"`
}

type SubmitScoreRequest struct {
	PlayerID string  `json:"player_id" binding:"required"`
	Score    float64 `json:"score" binding:"required"`
}

type LeaderboardEntry struct {
	Rank     int64   `json:"rank"`
	PlayerID string  `json:"player_id"`
	Score    float64 `json:"score"`
}
