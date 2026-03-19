using GradProject.Application.DTOs.Gamification;

namespace GradProject.Application.Interfaces;

/// <summary>
/// Central publisher for all achievement events.
/// Implementations persist events to the AchievementEvents table and can forward to
/// downstream systems (push notifications, AI commentary) in future sprints.
///
/// Callers must provide a stable DeduplicationKey. Duplicate keys are silently ignored —
/// the publisher guarantees at-most-once persistence per key.
///
/// This is the seam BE-6 (Notification API), MOB-3 (push), and AI-4 (commentary) hook into.
/// </summary>
public interface IAchievementEventPublisher
{
    Task PublishAsync(AchievementEventDto evt, CancellationToken ct = default);
}
