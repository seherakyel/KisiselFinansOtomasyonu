using System.ComponentModel.DataAnnotations;

namespace KisiselFinans.Core.Entities;

/// <summary>
/// Tasarruf Hedefi - "Araba için 50.000 TL biriktir" gibi hedefler
/// </summary>
public class SavingsGoal
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "";
    
    [MaxLength(255)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Hedef tutarı
    /// </summary>
    public decimal TargetAmount { get; set; }
    
    /// <summary>
    /// Şu ana kadar biriktirilen tutar
    /// </summary>
    public decimal CurrentAmount { get; set; }
    
    /// <summary>
    /// Hedef bitiş tarihi
    /// </summary>
    public DateTime? TargetDate { get; set; }
    
    /// <summary>
    /// Hedef ikonu (emoji)
    /// </summary>
    [MaxLength(10)]
    public string Icon { get; set; } = "🎯";
    
    /// <summary>
    /// Hedef rengi (hex)
    /// </summary>
    [MaxLength(7)]
    public string Color { get; set; } = "#6366F1";
    
    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Tamamlandı mı?
    /// </summary>
    public bool IsCompleted { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation
    public User? User { get; set; }
    
    // Computed
    public decimal ProgressPercentage => TargetAmount > 0 ? (CurrentAmount / TargetAmount) * 100 : 0;
    public decimal RemainingAmount => Math.Max(0, TargetAmount - CurrentAmount);
}

