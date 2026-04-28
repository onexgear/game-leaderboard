package service

import (
	"context"
	"fmt"

	"github.com/onexgear/game-leaderboard/model"
	"github.com/onexgear/game-leaderboard/repository"
)

type LeaderboardService interface {
	SubmitScore(ctx context.Context, playerID string, score float64) error
	GetLeaderboard(ctx context.Context, limit int64) ([]model.LeaderboardEntry, error)
	GetPlayerRank(ctx context.Context, playerID string) (*model.Player, error)
	RemovePlayer(ctx context.Context, playerID string) error
}

type leaderboardService struct {
	repo repository.LeaderboardRepository
}

func NewLeaderboardService(repo repository.LeaderboardRepository) LeaderboardService {
	return &leaderboardService{repo: repo}
}

func (s *leaderboardService) SubmitScore(ctx context.Context, playerID string, score float64) error {
	if playerID == "" {
		return fmt.Errorf("player_id cannot be empty")
	}
	if score < 0 {
		return fmt.Errorf("score cannot be negative")
	}
	return s.repo.SubmitScore(ctx, playerID, score)
}

func (s *leaderboardService) GetLeaderboard(ctx context.Context, limit int64) ([]model.LeaderboardEntry, error) {
	if limit <= 0 || limit > 100 {
		limit = 10
	}
	entries, err := s.repo.GetTopN(ctx, limit)
	if err != nil {
		return nil, err
	}

	result := make([]model.LeaderboardEntry, 0, len(entries))
	for i, e := range entries {
		result = append(result, model.LeaderboardEntry{
			Rank:     int64(i + 1),
			PlayerID: e.Member.(string),
			Score:    e.Score,
		})
	}
	return result, nil
}

func (s *leaderboardService) GetPlayerRank(ctx context.Context, playerID string) (*model.Player, error) {
	rank, err := s.repo.GetPlayerRank(ctx, playerID)
	if err != nil {
		return nil, err
	}
	score, err := s.repo.GetPlayerScore(ctx, playerID)
	if err != nil {
		return nil, err
	}
	return &model.Player{
		ID:    playerID,
		Score: score,
		Rank:  rank + 1, // convert 0-based to 1-based
	}, nil
}

func (s *leaderboardService) RemovePlayer(ctx context.Context, playerID string) error {
	return s.repo.RemovePlayer(ctx, playerID)
}
