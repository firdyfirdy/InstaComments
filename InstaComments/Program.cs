using InstaComments.Controller;
using InstaComments.Helpers;
using InstaComments.Model;
using InstagramApiSharp;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using System;
using System.Threading.Tasks;

namespace InstaComments
{
  class Program
  {
    private static bool isLogin = false;
    public static Random random = new Random();
    static async Task Main(string[] args)
    {
      var consoleRed = ConsoleColor.Red;
      var consoleGreen = ConsoleColor.Green;

      HelpersInstaApi.WriteFullLine("Instagram Auto Follows, Comments, and Likes!", consoleGreen);
      HelpersInstaApi.WriteFullLine("Contact me: me@firdy.dev", consoleGreen);
      Console.WriteLine();

      Console.Write("Username: ");
      string username = Console.ReadLine(); //"cumatupperware";
      Console.Write("Password: ");
      string password = Console.ReadLine(); //"Anonymous1704!";

      Console.Write("Target Username: ");
      string target = Console.ReadLine();  //"awkarin";
      Console.Write("Comments ( Delimeter with semicolon \";\" ): ");
      string captions = Console.ReadLine();  //"follback dong; follback kak!; follow back ya kak!; follow back ya!";
      Console.Write("Delay (in miliseconds): ");
      if (!int.TryParse(Console.ReadLine(), out int delay))
      {
        HelpersInstaApi.WriteFullLine($"Error, Not valid arguments.", consoleRed);
        return;
      }

      /* Set Instagram Session */
      UserSessionData sessionData = new UserSessionData()
      {
        UserName = username,
        Password = password
      };

      Actions instaActions = new Actions(sessionData);
      ActionModel login = await instaActions.DoLogin();

      HelpersInstaApi.WriteFullLine("Trying to login ...", consoleGreen);

      /* Login */
      HelpersInstaApi.WriteFullLine(login.Response);

      /* Login Success */
      if (login.Status == 1)
      {
        isLogin = true;
      }

      /* Login Challange */
      if (login.Status == 2)
      {
        await instaActions.SendCode();
        HelpersInstaApi.WriteFullLine("Put your code: ");
        string code = Console.ReadLine();

        ActionModel verifyCode = await instaActions.VerifyCode(code);
        HelpersInstaApi.WriteFullLine(verifyCode.Response);
        if (verifyCode.Status == 1)
          isLogin = true;
      }

      if (isLogin)
      {
        // Follow me (firdyfirdy)
        /*ActionModel follow = await instaActions.DoFollow(5600985630);*/
        string LatestMaxId = "";
        int i = 0;

        /* Get Target Informations */
        IResult<InstaUserInfo> targetInfo = await HelpersInstaApi.InstaApi.UserProcessor
          .GetUserInfoByUsernameAsync(target);
        if (targetInfo.Succeeded)
        {
          HelpersInstaApi.WriteFullLine($"Username: {targetInfo.Value.Username} " +
            $"| Followers: {targetInfo.Value.FollowerCount}");

          while (LatestMaxId != null)
          {
            /* Get Target Followers */
            IResult<InstaUserShortList> followersTarget = await HelpersInstaApi.InstaApi.UserProcessor
              .GetUserFollowersAsync(target, PaginationParameters.MaxPagesToLoad(1)
              .StartFromMaxId(LatestMaxId));
            if (followersTarget.Succeeded)
            {
              LatestMaxId = followersTarget.Value.NextMaxId;
              foreach (var follsUser in followersTarget.Value)
              {
                /* If account is not private */
                if (!follsUser.IsPrivate)
                {

                  /* get friendship status */
                  var getFriendshipStatus = await HelpersInstaApi.InstaApi.UserProcessor.GetFriendshipStatusAsync(follsUser.Pk);
                  if (getFriendshipStatus.Succeeded)
                  {
                    /* if they follow us */
                    if (getFriendshipStatus.Value.Following)
                    {
                      HelpersInstaApi.WriteFullLine($"[{i}] Username: {follsUser.UserName} | Skipped, Already Follow You.", consoleRed);
                    }
                    else if (getFriendshipStatus.Value.FollowedBy)
                    {
                      HelpersInstaApi.WriteFullLine($"[{i}] Username: {follsUser.UserName} | Skipped, You Already Follow.", consoleRed);
                    }
                    else
                    {
                      /* Get account media(s) */
                      IResult<InstaMediaList> userMedia = await HelpersInstaApi.InstaApi.UserProcessor
                        .GetUserMediaAsync(follsUser.UserName, PaginationParameters.MaxPagesToLoad(1));
                      if (userMedia.Succeeded)
                      {
                        /* If media is not empty*/
                        if (userMedia.Value.Count > 0)
                        {
                          /* Get first media */
                          InstaMedia firstMedia = userMedia.Value[0];

                          /* If media comment is not disabled */
                          if (!firstMedia.IsCommentsDisabled)
                          {
                            /* Like Media */
                            var like = await instaActions.DoLike(firstMedia.Pk);

                            /* Get random comments text */
                            string[] captionSplit = captions.Split(';');
                            int rnd = random.Next(0, captionSplit.Length);
                            string resultCaptions = captionSplit[rnd];

                            /* Comment media*/
                            var comment = await instaActions.DoComment(firstMedia, resultCaptions);
                            HelpersInstaApi.WriteFullLine($"[{i}] Username: {follsUser.UserName}");
                            Console.Write(" ");
                            if (comment.Status == 1)
                              HelpersInstaApi.WriteFullLine(comment.Response, consoleGreen);
                            if (comment.Status == 0)
                              HelpersInstaApi.WriteFullLine(comment.Response, consoleRed);

                            HelpersInstaApi.WriteFullLine($" [+] Sleep for {delay} ms");
                            await Task.Delay(delay);
                          }
                          else
                          {
                            HelpersInstaApi.WriteFullLine($"[{i}] Username: {follsUser.UserName} | Comment is disabled.", consoleRed);
                          }
                        }
                        else
                        {
                          HelpersInstaApi.WriteFullLine($"[{i}] Username: {follsUser.UserName} | Media is empty.", consoleRed);
                        }
                      }
                      else
                      {
                        HelpersInstaApi.WriteFullLine($"[{i}] Username: {follsUser.UserName} | {userMedia.Info.Message}", consoleRed);
                      }
                    }
                  }
                }
                else
                {
                  HelpersInstaApi.WriteFullLine($"[{i}] Username: {follsUser.UserName} | Account is private.", consoleRed);
                }
                i++;
              }
            }
          }
        }
      }
    }
  }
}
