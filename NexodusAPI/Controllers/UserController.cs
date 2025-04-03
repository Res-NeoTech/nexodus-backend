using System.Web;
using Microsoft.AspNetCore.Mvc;
using NexodusAPI.Models;
using NexodusAPI.Utils;
using MongoDB.Driver;
using System.ComponentModel.DataAnnotations;
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
            if (user == null)
            {
                return BadRequest("User data is null.");
            }

            if (!IsValidUser(user, out var validationMessage))
            {
                return BadRequest(validationMessage);
            }

            try
            {
                user.Email = user.Email.Trim().ToLower();
                user.Name = HttpUtility.HtmlEncode(user.Name).Trim();

                //Checks if the email is taken.
                bool emailExists = await _userContext.Users.Find(u => u.Email == user.Email).AnyAsync();

                if (emailExists)
                {
                    return Conflict("A user with this email already exists.");
                }

                user.Password = Cryptography.HashPassword(user.Password);

                await _userContext.Users.InsertOneAsync(user);

                var responseUser = new
                {
                    user.Id,
                    user.Name,
                    user.Email
                };

                return CreatedAtAction(nameof(CreateUser), new { id = user.Id }, responseUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while creating the user.");
            }
        }

        private bool IsValidUser(User user, out string message)
        {
            var emailAddress = new EmailAddressAttribute();
            string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$";

            if (string.IsNullOrWhiteSpace(user.Name) || string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Password))
            {
                message = "Some data is missing.";
                return false;
            }

            if (user.Name.Length < 3 || user.Name.Length > 50)
            {
                message = "Name must be between 3 and 50 characters.";
                return false;
            }

            if (!emailAddress.IsValid(user.Email) || user.Email.Length > 100)
            {
                message = "Email is not valid.";
                return false;
            }

            if (!Regex.IsMatch(user.Password, passwordPattern))
            {
                message = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number, and one special character.";
                return false;
            }

            message = string.Empty;
            return true;
        }
    }
}
