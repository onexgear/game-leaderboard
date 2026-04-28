package handler

import (
	"net/http"
	"strconv"

	"github.com/gin-gonic/gin"
	"github.com/onexgear/game-leaderboard/model"
	"github.com/onexgear/game-leaderboard/service"
)

type LeaderboardHandler struct {
	svc service.LeaderboardService
}

func NewLeaderboardHandler(svc service.LeaderboardService) *LeaderboardHandler {
	return &LeaderboardHandler{svc: svc}
}

func (h *LeaderboardHandler) RegisterRoutes(r *gin.Engine) {
	v1 := r.Group("/api/v1")
	{
		v1.POST("/scores", h.SubmitScore)
		v1.GET("/leaderboard", h.GetLeaderboard)
		v1.GET("/players/:id/rank", h.GetPlayerRank)
		v1.DELETE("/players/:id", h.RemovePlayer)
	}
}

func (h *LeaderboardHandler) SubmitScore(c *gin.Context) {
	var req model.SubmitScoreRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	if err := h.svc.SubmitScore(c.Request.Context(), req.PlayerID, req.Score); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	c.JSON(http.StatusOK, gin.H{"message": "score submitted", "player_id": req.PlayerID, "score": req.Score})
}

func (h *LeaderboardHandler) GetLeaderboard(c *gin.Context) {
	limitStr := c.DefaultQuery("limit", "10")
	limit, err := strconv.ParseInt(limitStr, 10, 64)
	if err != nil || limit <= 0 {
		limit = 10
	}

	entries, err := h.svc.GetLeaderboard(c.Request.Context(), limit)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	c.JSON(http.StatusOK, gin.H{"leaderboard": entries, "total": len(entries)})
}

func (h *LeaderboardHandler) GetPlayerRank(c *gin.Context) {
	playerID := c.Param("id")

	player, err := h.svc.GetPlayerRank(c.Request.Context(), playerID)
	if err != nil {
		c.JSON(http.StatusNotFound, gin.H{"error": err.Error()})
		return
	}

	c.JSON(http.StatusOK, player)
}

func (h *LeaderboardHandler) RemovePlayer(c *gin.Context) {
	playerID := c.Param("id")

	if err := h.svc.RemovePlayer(c.Request.Context(), playerID); err != nil {
		c.JSON(http.StatusNotFound, gin.H{"error": err.Error()})
		return
	}

	c.JSON(http.StatusOK, gin.H{"message": "player removed", "player_id": playerID})
}
