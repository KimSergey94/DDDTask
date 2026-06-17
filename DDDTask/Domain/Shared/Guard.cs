namespace DDDTask.Domain.Shared;

public static class Guard
{
    public static void Against(bool condition, string message)
    {
        if (condition) throw new DomainException(message);
    }
}
