using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RavenWEB.Extensions;
using RavenWEB.Models;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace RavenWEB.Controllers
{
    [Route("[controller]")]
    public class LoginController : Controller
    {
        public IConfiguration Config { get; }

        public LoginController(IConfiguration config)
        {
            Config = config;
        }


        [HttpGet]  
        public IActionResult Login()
        {
            return Redirect($"https://discordapp.com/api/oauth2/authorize?client_id={Config["Discord:AppId"]}&redirect_uri={Config["Discord:RedirectUrl"]}&response_type=code&scope=guilds");
        }
    }
}
