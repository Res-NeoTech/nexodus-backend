using System.Web;
using Microsoft.AspNetCore.Mvc;
using NexodusAPI.Models;
using NexodusAPI.Utils;
using MongoDB.Driver;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NexodusAPI.Controllers
{
    [Route("crud/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserContext _userContext;

        /// <summary>
        /// Constructor for UserController.
        /// </summary>
        /// <param name="userContext"></param>
        public UserController(UserContext userContext)
        {
            _userContext = userContext;
        }

        /// <summary>
        /// Creates a new user and inserts in a database.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
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
                user.Token = Cryptography.GenerateToken(user.Email);

                await _userContext.Users.InsertOneAsync(user);

                var responseUser = new
                {
                    user.Token
                };

                return CreatedAtAction(nameof(CreateUser), new { id = user.Id }, responseUser);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while creating the user.");
            }
        }

        /// <summary>
        /// Authentificates user by login and password.
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("/crud/auth")]
        public async Task<IActionResult> Authentificate([FromBody] Login login)
        {
            if(login == null)
            {
                return BadRequest("Login credentials are null.");
            }

            if(string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrWhiteSpace(login.Password))
            {
                return BadRequest("Email or password is missing.");
            }

            if(!VerifyCredentials(login.Email, login.Password))
            {
                return Unauthorized("Invalid email or password.");
            } 
            else
            {
                User user = await _userContext.Users.Find(u => u.Email == login.Email).FirstOrDefaultAsync();

                return Ok(new
                {
                    user.Token
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUser([FromHeader(Name = "Authorization")] string nexodusToken)
        {
            if (nexodusToken.StartsWith("Nexodus "))
            {
                nexodusToken = nexodusToken.Substring(8);

                User user = await _userContext.Users.Find(u => u.Token == nexodusToken).FirstOrDefaultAsync();

                return user != null ? Ok(user) : NotFound("User not found.");
            } else
            {
                return BadRequest("Invalid token format.");
            }
        }

        /// <summary>
        /// Validates the user data that was sent from a front-end.
        /// </summary>
        /// <param name="user">User class to validate.</param>
        /// <param name="message">Error message.</param>
        /// <returns>True if the entered credentials are valid. False if not.</returns>
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

        /// <summary>
        /// Verifies user by entered email and password.
        /// </summary>
        /// <param name="email">user-entered email.</param>
        /// <param name="password">user-entered password</param>
        /// <returns>True if user exists and password is correct. False if not.</returns>
        private bool VerifyCredentials(string email, string password)
        {
            User user = _userContext.Users.Find(u => u.Email == email).FirstOrDefault();

            if (user == null)
            {
                return false;
            }
            else
            {
                return Cryptography.VerifyPassword(password, user.Password);
            }
        }
    }
}
