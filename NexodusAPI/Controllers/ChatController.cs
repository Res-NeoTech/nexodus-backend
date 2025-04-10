using Microsoft.AspNetCore.Http;
using NexodusAPI.Models;
using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace NexodusAPI.Controllers
{
    [Route("chats/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ChatContext _chatContext;
        private readonly UserContext _userContext;

        /// <summary>
        /// Constructor for ChatController.
        /// </summary>
        /// <param name="chatContext"></param>
        public ChatController(ChatContext chatContext, UserContext userContext)
        {
            _chatContext = chatContext;
            _userContext = userContext;
        }

        /// <summary>
        /// Creates a new chat that starts with a user prompt.
        /// </summary>
        /// <param name="message">Message class. The role parameter for this method must always be the user.</param>
        /// <param name="nexodusToken">Nexodus authentication token.</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CreateChat([FromBody] Message message, [FromHeader(Name = "Authorization")] string nexodusToken)
        {
            if(message == null)
            {
                return BadRequest("Message is null.");
            }

            if(message.Role != "user" || string.IsNullOrWhiteSpace(message.Content))
            {
                return BadRequest("Some parameters are either incorrect or missing.");
            }

            if (await AuthenticateByToken(nexodusToken))
            {
                string token = nexodusToken.Substring(8);
                string userId = (await _userContext.Users.Find(u => u.Token == token).FirstOrDefaultAsync()).Id;
                message.Content = HttpUtility.HtmlEncode(message.Content).Trim();

                Chat newChat = new();
                newChat.UserId = userId;
                newChat.Messages.Add(message);

                await _chatContext.Chats.InsertOneAsync(newChat);

                var responseChat = new
                {
                    newChat.Id
                };

                return CreatedAtAction(nameof(CreateChat), new { id = newChat.Id }, responseChat);
            }
            else 
            { 
                return Unauthorized("Invalid format or unknown user.");
            }
        }

        /// <summary>
        /// Obtains complete chat information if this chat belongs to the authenticated user.
        /// </summary>
        /// <param name="nexodusToken">Nexodus authentication token.</param>
        /// <param name="chatId">Id of the requested chat.</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetChat([FromHeader(Name = "Authorization")] string nexodusToken, [FromQuery(Name = "id")]string chatId)
        {
            if (await AuthenticateByToken(nexodusToken)) 
            {
                string token = nexodusToken.Substring(8);
                string userId = (await _userContext.Users.Find(u => u.Token == token).FirstOrDefaultAsync()).Id;
                Chat requestedChat;

                try
                {
                    requestedChat = await _chatContext.Chats.Find(c => c.Id == chatId).FirstOrDefaultAsync();
                }
                catch (Exception ex) 
                { 
                    return BadRequest(ex.Message);
                }

                if (requestedChat != null) 
                {
                    if (await AuthorizeUser(userId, requestedChat.Id)) 
                    {
                        return Ok(requestedChat);
                    } 
                    else
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, "User doesn't have permission to access this resource.");
                    }
                } 
                else
                {
                    return NotFound("Requested chat doesn't exist.");
                }
            } 
            else
            {
                return Unauthorized("Invalid format or unknown user.");
            }
        }

        /// <summary>
        /// Authenticates user by the provided token.
        /// </summary>
        /// <param name="token">Nexodus authentication token.</param>
        /// <returns>True if the user with this token exists. False if the token format is incorrect or the user with the provided token is not found.</returns>
        private async Task<bool> AuthenticateByToken(string token) 
        {
            if (token.StartsWith("Nexodus "))
            {
                token = token.Substring(8);

                User user = await _userContext.Users.Find(u => u.Token == token).FirstOrDefaultAsync();

                return user != null ? true : false;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Authorizes user to access the chat.
        /// </summary>
        /// <param name="userId">Id of a user trying to obtain access.</param>
        /// <param name="chatId">Id of a access-requested chat.</param>
        /// <returns>True if the user is eligible to access the chat. False if not.</returns>
        public async Task<bool> AuthorizeUser(string userId, string chatId)
        {
            Chat chat = await _chatContext.Chats.Find(c => c.UserId == userId && c.Id == chatId).FirstOrDefaultAsync();
            return chat != null;
        }
    }
}
