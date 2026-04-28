package repository

import (
	"context"
	"fmt"

	"github.com/redis/go-redis/v9"
)

const leaderboardKey = "game:leaderboard"

type LeaderboardRepository interface {
	SubmitScore(ctx context.Context, playerID string, score float64) error
	GetTopN(ctx context.Context, n int64) ([]redis.Z, error)
	GetPlayerRank(ctx context.Context, playerID string) (int64, error)
	GetPlayerScore(ctx context.Context, playerID string) (float64, error)
	RemovePlayer(ctx context.Context, playerID string) error
}

type redisRepo struct {
	client *redis.Client
}

func NewLeaderboardRepository(client *redis.Client) LeaderboardRepository {
	return &redisRepo{client: client}
}

// SubmitScore uses ZADD with GT flag: only updates if new score is greater than current.
func (r *redisRepo) SubmitScore(ctx context.Context, playerID string, score float64) error {
	return r.client.ZAddGT(ctx, leaderboardKey, redis.Z{
		Score:  score,
		Member: playerID,
	}).Err()
}

// GetTopN returns top N players sorted by score descending.
func (r *redisRepo) GetTopN(ctx context.Context, n int64) ([]redis.Z, error) {
	return r.client.ZRevRangeWithScores(ctx, leaderboardKey, 0, n-1).Result()
}

// GetPlayerRank returns 0-based rank from top (0 = #1). Returns -1 if not found.
func (r *redisRepo) GetPlayerRank(ctx context.Context, playerID string) (int64, error) {
	rank, err := r.client.ZRevRank(ctx, leaderboardKey, playerID).Result()
	if err == redis.Nil {
		return -1, fmt.Errorf("player %s not found", playerID)
	}
	return rank, err
}

func (r *redisRepo) GetPlayerScore(ctx context.Context, playerID string) (float64, error) {
	score, err := r.client.ZScore(ctx, leaderboardKey, playerID).Result()
	if err == redis.Nil {
		return 0, fmt.Errorf("player %s not found", playerID)
	}
	return score, err
}

func (r *redisRepo) RemovePlayer(ctx context.Context, playerID string) error {
	removed, err := r.client.ZRem(ctx, leaderboardKey, playerID).Result()
	if err != nil {
		return err
	}
	if removed == 0 {
		return fmt.Errorf("player %s not found", playerID)
	}
	return nil
}
