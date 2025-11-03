namespace Api.Dtos;

public class StartMatchRequest
{
    public int CourtId { get; set; }
    public string Mode { get; set; } = "Singles"; // Singles or Doubles
}

public class FinishMatchRequest
{
    public int MatchId { get; set; }
    public string ScoreText { get; set; } = ""; // e.g., "21-17, 18-21, 21-19"
}
