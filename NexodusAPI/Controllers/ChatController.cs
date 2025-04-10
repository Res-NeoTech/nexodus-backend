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

            if(message.Role != "user" || string.IsNullOrWhiteSpace(message.Content)) // In a priori it's always the user who initiates the conversation. That's why at creating the new chat, user role is the first one.
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
                newChat.Title = "New chat";
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
        /// Obtains chatIds of chats that belongs to user.
        /// </summary>
        /// <param name="nexodusToken">Nexodus authentication token.</param>
        /// <returns></returns>
        [HttpGet]
        [Route("/chats/list")]
        public async Task<IActionResult> GetAllChats([FromHeader(Name = "Authorization")] string nexodusToken)
        {
            if(await AuthenticateByToken(nexodusToken))
            {
                string token = nexodusToken.Substring(8);
                string userId = (await _userContext.Users.Find(u => u.Token == token).FirstOrDefaultAsync()).Id;

                List<Chat> chats = await _chatContext.Chats.Find(c => c.UserId == userId).ToListAsync();
                List<ChatDTO> displayedChats = new();

                for (int i = 0; i < chats.Count; i++)
                {
                    displayedChats.Add(new ChatDTO(chats[i].Id, chats[i].Title));
                }

                return Ok(displayedChats);
            }
            else
            {
                return Unauthorized("Invalid format or unknown user.");
            }
        }

        /// <summary>
        /// Updates the chat title if this chat belongs to the authenticated user.
        /// </summary>
        /// <param name="update">New chat name.</param>
        /// <param name="nexodusToken">Nexodus authentication token.</param>
        /// <returns></returns>
        [HttpPut]
        public async Task<IActionResult> UpdateChatTitle([FromBody] UpdateChat update, [FromHeader(Name = "Authorization")] string nexodusToken)
        {
            if(update == null)
            {
                return BadRequest("Update info is null.");
            }

            if(string.IsNullOrWhiteSpace(update.Id) || string.IsNullOrWhiteSpace(update.Title))
            {
                return BadRequest("Some parameters are either incorrect or missing.");
            }

            if (await AuthenticateByToken(nexodusToken)) 
            {
                string token = nexodusToken.Substring(8);
                string userId = (await _userContext.Users.Find(u => u.Token == token).FirstOrDefaultAsync()).Id;
                Chat requestedChat;

                try
                {
                    requestedChat = await _chatContext.Chats.Find(c => c.Id == update.Id).FirstOrDefaultAsync();
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }

                if (requestedChat != null)
                {
                    if (await AuthorizeUser(userId, requestedChat.Id))
                    {
                        update.Title = HttpUtility.HtmlEncode(update.Title).Trim();
                        requestedChat.Title = update.Title;

                        await _chatContext.Chats.ReplaceOneAsync(c => c.Id == requestedChat.Id, requestedChat);
                        return Ok("Chat Title was changed with success.");
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
        /// Appends new message to a chat if it belongs to the authenticated user.
        /// </summary>
        /// <param name="newMessage">Message class to append.</param>
        /// <param name="nexodusToken">Nexodus authentication token.</param>
        /// <param name="chatId">Id of the requested chat.</param>
        /// <returns></returns>
        [HttpPut]
        [Route("/chats/append")]
        public async Task<IActionResult> AppendChatContent([FromBody] Message newMessage, [FromHeader (Name = "Authorization")] string nexodusToken, [FromQuery(Name = "id")] string chatId)
        {
            if (newMessage == null)
            {
                return BadRequest("Message is null.");
            }

            if(string.IsNullOrWhiteSpace(newMessage.Content) || !new[] { "user", "assistant" }.Contains(newMessage.Role)) // Only user and assistant roles are accepted via Mistral API. We aren't using booleans in the case Mistral API will support more roles in the future.
            {
                return BadRequest("Some parameters are either incorrect or missing.");
            }

            if(await AuthenticateByToken(nexodusToken))
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
                        newMessage.Content = HttpUtility.HtmlEncode(newMessage.Content).Trim();
                        requestedChat.Messages.Add(newMessage);

                        await _chatContext.Chats.ReplaceOneAsync(c => c.Id == requestedChat.Id, requestedChat);

                        return Ok("New message was succesfully appended.");
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