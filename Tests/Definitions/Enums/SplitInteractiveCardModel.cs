using System;
using UnityEngine;

[Serializable]
public class SplitInteractiveCardModel
{
    public Texture Image;
    public string Title;
    public DurationSelection Duration = DurationSelection.Minutes10; // Duration in minutes
    public AccessType AccessType;
    public AudioType Type;
    public string Location;
    public System.DateTime StartDateTime;
    [TextArea]
    public string Description;
    public User Host;
    public User[] Participants;

    public RepeatSchedule RepeatSchedule = RepeatSchedule.None;
    public int ParticipantCount;
    public ScheduleType ScheduleType;
    public CentralObjectType CentralObject;


    /// <summary>
    /// Gets the end time of this event (StartDateTime + Duration in minutes).
    /// </summary>
    public System.DateTime EndDateTime => StartDateTime.AddMinutes((int)Duration);

    /// <summary>
    /// Returns true if the event has already ended.
    /// </summary>
    public bool HasEnded => EndDateTime <= System.DateTime.Now;

    /// <summary>
    /// Returns true if the event is currently happening (started but not yet ended).
    /// </summary>
    public bool IsHappeningNow => StartDateTime <= System.DateTime.Now && !HasEnded;

    /// <summary>
    /// Returns true if the event is either happening now or in the future (hasn't ended).
    /// </summary>
    public bool IsActive => !HasEnded;
}

