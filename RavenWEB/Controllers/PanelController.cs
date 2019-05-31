using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RavenWEB.Extensions;
using RavenWEB.Models;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace RavenWEB.Controllers
{
    [Route("[controller]")]
    public class PanelController : Controller
    {
        public IConfiguration Config { get; }

        public PanelController(IConfiguration config)
        {
            Config = config;
        }

        public UserAccessToken GetAccessToken(string code)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://discordapp.com/api/oauth2/token");
            webRequest.Method = "POST";
            string parameters = "client_id=" + Config["Discord:AppId"] + "&client_secret=" + Config["Discord:AppSecret"] + "&grant_type=authorization_code&code=" + code + "&redirect_uri=" + Config["Discord:RedirectUrl"];
            byte[] byteArray = Encoding.UTF8.GetBytes(parameters);
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = byteArray.Length;
            Stream postStream = webRequest.GetRequestStream();
 
            postStream.Write(byteArray, 0, byteArray.Length);
            postStream.Close();
            WebResponse response = webRequest.GetResponse();
            postStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(postStream);
            string responseFromServer = reader.ReadToEnd();

            try
            {
                var responseObject = JsonConvert.DeserializeObject<UserAccessToken>(responseFromServer);
                return responseObject;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Panel([FromQuery(Name = "code")] string code)
        {
            var token = GetAccessToken(code);

            var claims = new List<Claim>  
            {  
                new Claim(ClaimTypes.Name, token.access_token)  
            };  
            ClaimsIdentity userIdentity = new ClaimsIdentity(claims, "login");  
            ClaimsPrincipal principal = new ClaimsPrincipal(userIdentity);

            await HttpContext.SignInAsync(principal);
            return RedirectToAction("UserHome", "User");
            //return Redirect($"{apiuri}success?token={token}");
        }
    }
}
