﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TennisSim.Data;
using TennisSim.Models;
namespace TennisSim.Controllers
{
    public class GameStartController : Controller
    {
        private readonly ApplicationDbContext _context;
        public GameStartController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult EnterUsername()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> EnterUsername(UserName userName1)
        {
            if (ModelState.IsValid)
            {
                bool usernameExists = await _context.UserNames
                    .AnyAsync(u => u.Username == userName1.Username);

                if (usernameExists)
                {
                    ModelState.AddModelError("Username", "This username is already taken.");
                    return View(userName1);
                }

                _context.UserNames.Add(userName1);
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("Username", userName1.Username);
                return RedirectToAction("New");
            }
            return View(userName1);
        }
        public IActionResult New()
        {
            string? username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("EnterUsername", "GameStart");
            }
            UserName model = new UserName { Username = username };
            return View(model);
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("Username");
            return RedirectToAction("EnterUsername");
        }
    }

}