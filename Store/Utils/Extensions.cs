using System;
namespace Utils
{
    public static class Extensions
    {
        public static void ToTrimmedLowerCaseValues(this string[] array) 
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = array[i].Trim().ToLower();
            }
        }
    }
}
