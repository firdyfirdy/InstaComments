using InstagramApiSharp.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace InstaComments.Helpers
{
  public static class HelpersInstaApi
  {
    public static IInstaApi InstaApi { get; set; }
    
    public static void WriteFullLine(string value, ConsoleColor color = ConsoleColor.DarkGreen)
    {
      //
      // This method writes an entire line to the console with the string.
      //
      Console.ForegroundColor = color;
      Console.Write(value);
      Console.ResetColor();
      Console.WriteLine();
    }
  }
}
