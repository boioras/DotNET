namespace ToDoListProgram.Data;

// (subtype polymorphism via inheritance + overriding)
public abstract record Priority(string Code)
{
    public abstract string BadgeClass { get; }
}

public record High()   : Priority("H") { public override string BadgeClass => "bg-danger"; }
public record Medium() : Priority("M") { public override string BadgeClass => "bg-warning text-dark"; }
public record Low() : Priority("L") { public override string BadgeClass => "bg-success"; }
// Fallback for unrecognised/empty codes
public record None()   : Priority("")  { public override string BadgeClass => "bg-secondary"; }

public static class PriorityFactory
{
    // Creates the correct subtype from a raw string
    public static Priority From(string? code) =>
        code?.ToUpper() switch
        {
            "H" => new High(),
            "M" => new Medium(),
            "L" => new Low(),
            _ => new None()
        };
}