using InstaComments.Helpers;
using InstaComments.Model;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Logger;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InstaComments.Controller
{
  public class Actions
  {
    private readonly UserSessionData userSession;

    public Actions(UserSessionData UserSession)
    {
      userSession = UserSession;
    }

    public async Task<ActionModel> DoLogin(HttpClientHandler httpClient = null)
    {
      ActionModel dataAction = new ActionModel
      {
        Type = "Login",
        Username = userSession.UserName,
        Status = 0
      };
      try
      {
        IInstaApiBuilder instaApiBuild = InstaApiBuilder.CreateBuilder()
            .SetUser(userSession)
            .UseLogger(new DebugLogger(LogLevel.Exceptions));

        if (httpClient != null)
        {
          instaApiBuild = instaApiBuild.UseHttpClientHandler(httpClient);
        }

        HelpersInstaApi.InstaApi = instaApiBuild.Build();

        var loginResult = await HelpersInstaApi.InstaApi.LoginAsync();
        if (loginResult.Succeeded)
        {
          dataAction.Status = 1;
          dataAction.Response = $"[+] Login | Status: Success | Username: {userSession.UserName}";
        }
        else
        {
          switch (loginResult.Value)
          {
            case InstaLoginResult.InvalidUser:
              dataAction.Response = $"[+] Login | Status: Failed | Error: Username is invalid.";
              break;
            case InstaLoginResult.BadPassword:
              dataAction.Response = $"[+] Login | Status: Failed | Error: Password is wrong.";
              break;
            case InstaLoginResult.Exception:
              dataAction.Response = $"[+] Login | Status: Failed | Error: {loginResult.Info.Message}";
              break;
            case InstaLoginResult.LimitError:
              dataAction.Response = $"[+] Login | Status: Failed | Error: Limit error (you should wait 10 minutes).";
              break;
            case InstaLoginResult.ChallengeRequired:
              dataAction.Status = 2;
              dataAction.Response = $"[+] Login | Status: Failed | Error: Challenge Required.";
              HandleChallenge();
              break;
            case InstaLoginResult.TwoFactorRequired:
              dataAction.Response = $"[+] Login | Status: Failed | Error:  Factor Required. Disabled it first!";
              break;
            case InstaLoginResult.InactiveUser:
              dataAction.Response = $"[+] Login | Status: Failed | Error:  {loginResult.Info.Message}";
              break;
          }
        }
      }
      catch (Exception ex)
      {
        dataAction.Response = $"[+] Login | Status: Failed | Error:  {ex.Message}";
      }
      return dataAction;
    }

    private static async void HandleChallenge()
    {
      try
      {
        IResult<InstaChallengeRequireVerifyMethod> challenge = null;
        challenge = await HelpersInstaApi.InstaApi.GetChallengeRequireVerifyMethodAsync();
        if (challenge.Succeeded)
        {
          if (challenge.Value.StepData != null)
          {
            if (!string.IsNullOrEmpty(challenge.Value.StepData.PhoneNumber))
              Console.WriteLine($"[ 1 ] Challange: {challenge.Value.StepData.PhoneNumber}");
            if (!string.IsNullOrEmpty(challenge.Value.StepData.Email))
              Console.WriteLine($"[ 2 ] Challange: {challenge.Value.StepData.Email}");
          }
        }
        else
        {
          Console.WriteLine($"Challange Error: {challenge.Info.Message}");
          return;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Challange Error: {ex.Message}");
        return;
      }
    }

    public async Task SendCode()
    {
      Console.Write("Verify options [1 or 2]: ");
      var options = Console.ReadLine();
      if (HelpersInstaApi.InstaApi == null)
        return;
      try
      {
        if (int.TryParse(options, out int option))
        {
          if (option == 1)
          {
            var phoneNumber = await HelpersInstaApi.InstaApi.RequestVerifyCodeToSMSForChallengeRequireAsync();
            if (phoneNumber.Succeeded)
            {
              Console.WriteLine($"We sent verify code to this phone number: {phoneNumber.Value.StepData.ContactPoint}");
            }
            else
            {
              Console.WriteLine($"Challange Error: {phoneNumber.Info.Message}");
              return;
            }
          }
          else if (option == 2)
          {
            var email = await HelpersInstaApi.InstaApi.RequestVerifyCodeToEmailForChallengeRequireAsync();
            if (email.Succeeded)
            {
              Console.WriteLine($"We sent verify code to this Email: {email.Value.StepData.ContactPoint}");
            }
            else
            {
              Console.WriteLine($"Challange Error: {email.Info.Message}");
              return;
            }
          }
          else
          {
            Console.WriteLine("Invalid input!");
            return;
          }
        }
        else
        {
          Console.WriteLine("Invalid input , Enter only number");
          return;
        }
      }
      catch (Exception ex)
      {

        Console.WriteLine($"Challange Error: {ex.Message}");
        return;
      }
    }

    public async Task<ActionModel> VerifyCode(string code)
    {
      ActionModel dataAction = new ActionModel
      {
        Type = "VerifyCode",
        Username = userSession.UserName,
        Status = 0
      };
      try
      {
        var regex = new Regex(@"^-*[0-9,\.]+$");
        if (!regex.IsMatch(code))
        {
          dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: Verification code is numeric!";
        }
        if (code.Length != 6)
        {
          dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: Verification code must be 6 digits!";
        }
        var verify = await HelpersInstaApi.InstaApi.VerifyCodeForChallengeRequireAsync(code);
        if (verify.Succeeded)
        {
          dataAction.Status = 1;
          dataAction.Response = $"[+] {dataAction.Type} | Status: Success | Username: {userSession.UserName} { Environment.NewLine}";
        }
        else
        {
          if (verify.Value == InstaLoginResult.TwoFactorRequired)
          {
            dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: Two Factor Required. Disabled it first!";
          }
        }
      }
      catch (Exception ex)
      {
        dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: {ex.Message}!";
      }

      return dataAction;
    }

    public async Task<ActionModel> DoFollow(long userPk)
    {
      ActionModel dataAction = new ActionModel
      {
        Type = "Follow",
        Username = null,
        Status = 0
      };
      try
      {
        var follow = await HelpersInstaApi.InstaApi.UserProcessor.FollowUserAsync(userPk);
        if (follow.Succeeded)
        {
          dataAction.Status = 1;
          dataAction.Response = $"[+] {dataAction.Type} | Status: Success";
        }
        else
        {
          dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: {follow.Info.Message}";
        }

      }
      catch (Exception ex)
      {

        dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: {ex.Message}";
      }
      return dataAction;
    }

    public async Task<ActionModel> DoLike(string mediaPk)
    {
      ActionModel dataAction = new ActionModel
      {
        Type = "Like",
        Username = userSession.UserName,
        Status = 0
      };
      try
      {
        var doLike = await HelpersInstaApi.InstaApi.MediaProcessor.LikeMediaAsync(mediaPk);
        if (doLike.Succeeded)
        {
          dataAction.Status = 1;
          dataAction.Response = $"[+] {dataAction.Type} | Status: Success";
        }
        else
        {
          dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: {doLike.Info.Message}";
        }

      }
      catch (Exception ex)
      {
        dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: {ex.Message}";
      }
      return dataAction;
    }

    public async Task<ActionModel> DoComment(InstaMedia media, string comments)
    {
      ActionModel dataAction = new ActionModel
      {
        Type = "Comment",
        Username = userSession.UserName,
        Status = 0
      };

      try
      {
        var comment = await HelpersInstaApi.InstaApi.CommentProcessor.CommentMediaAsync(media.Pk, comments);
        if (comment.Succeeded)
        {
          dataAction.Status = 1;
          dataAction.Response = $"[+] {dataAction.Type} | Status: Success | Page: https://instagram.com/p/{media.Code} | Text: {comment.Value.Text}";
        }
        else
        {
          dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: {comment.Info.Message}";
        }

      }
      catch (Exception ex)
      {
        dataAction.Response = $"[+] {dataAction.Type} | Status: Failed | Error: {ex.Message}";
      }

      return dataAction;
    }
  }
}
