using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using NexodusAPI.Models;
using NexodusAPI.Utils;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace NexodusAPI.Controllers
{
    [Route("crud/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserContext _userContext;

        public UserController(UserContext userContext)
        {
            _userContext = userContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            var emailAddress = new EmailAddressAttribute();
            string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$";

            if (user == null)
            {
                return BadRequest("User data is null.");
            }

            if (string.IsNullOrEmpty(user.Name) || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
            {
                return BadRequest("Some data is missing.");
            }

            user.Name = HttpUtility.HtmlEncode(user.Name).Trim();
            user.Email = HttpUtility.HtmlEncode(user.Email).Trim().ToLower();

            if (user.Name.Length < 3 || user.Name.Length > 50)
            {
                return BadRequest("Name must be between 3 and 50 characters.");
            }

            if(!emailAddress.IsValid(user.Email) || user.Email.Length > 100)
            {
                return BadRequest("Email is not valid.");
            }

            if(!Regex.IsMatch(user.Password, passwordPattern))
            {
                return BadRequest("Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
            }

            // Verify if the email is already taken.
            var existingUser = await _userContext.Users.Find(u => u.Email == user.Email).FirstOrDefaultAsync();

            if (existingUser != null)
            {
                return Conflict("A user with this email already exists.");
            }

            user.Password = Password.HashPassword(user.Password);

            // Creates the user.
            await _userContext.Users.InsertOneAsync(user);

            return CreatedAtAction(nameof(CreateUser), new { id = user.Id }, user);
        }
    }
}
