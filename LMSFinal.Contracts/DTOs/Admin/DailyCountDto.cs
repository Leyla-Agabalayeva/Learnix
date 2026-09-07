namespace LMSFinal.Contracts.DTOs.Admin
{
    /// <summary>Date — "yyyy-MM-dd", готова для подписи оси на графике без доп. форматирования.</summary>
    public record DailyCountDto(string Date, int Count);
}
