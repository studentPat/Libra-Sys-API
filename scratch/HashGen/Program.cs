using Microsoft.AspNetCore.Identity;
using System;

class Program
{
    static void Main()
    {
        var hasher = new PasswordHasher<object>();
        var user = new object();
        
        string librarianHash = hasher.HashPassword(user, "Librarian123!");
        string memberHash = hasher.HashPassword(user, "Member123!");
        
        Console.WriteLine($"demo_librarian hash (Password: Librarian123!):");
        Console.WriteLine(librarianHash);
        Console.WriteLine();
        Console.WriteLine($"demo_member hash (Password: Member123!):");
        Console.WriteLine(memberHash);
    }
}
