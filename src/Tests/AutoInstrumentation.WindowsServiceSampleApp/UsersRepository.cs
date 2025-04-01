namespace AutoInstrumentation.WindowsServiceSampleApp;

interface IUsersRepository
{
    string[] GetAllUsers();
}

class UsersRepository : IUsersRepository
{
    public string[] GetAllUsers()
    {
        Thread.Sleep(Random.Shared.Next(100,200));
        return ["ed", "edd", "eddy"];
    }
}