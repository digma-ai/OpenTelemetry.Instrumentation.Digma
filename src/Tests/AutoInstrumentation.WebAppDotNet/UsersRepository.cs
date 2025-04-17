namespace AutoInstrumentation.WebAppDotNet;

interface IUsersRepository
{
    string[] GetAllUsers();
}

class UsersRepository : IUsersRepository
{
    private static readonly Random _random = new();
    
    public string[] GetAllUsers()
    {
        Thread.Sleep(_random.Next(100,200));
        return new[]{"ed", "edd", "eddy"};
    }
}