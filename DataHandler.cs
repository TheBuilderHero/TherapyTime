using System;
using System.IO;

public static class DataHandler
{
    public static string GetDataFilePath(string fileName)
    {
        // Get the path to the LocalApplicationData folder
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Define a subfolder for your specific application to keep things organized
        string appFolderPath = Path.Combine(appDataPath, "TherapyTime"); 

        // Ensure the directory exists
        if (!Directory.Exists(appFolderPath))
        {
            Directory.CreateDirectory(appFolderPath);
        }

        // Combine the folder path with your file name
        string fullPath = Path.Combine(appFolderPath, fileName);

        return fullPath;
    }

    public static string GetPersistentStudentsFilePath()
    {
        return GetDataFilePath("students.json");
    }
}