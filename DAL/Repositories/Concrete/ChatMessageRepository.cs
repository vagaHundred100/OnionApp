using DAL.Context;
using DAL.Domains;
using DAL.Helpers;
using DAL.Repositories.Abstract;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories.Concrete
{
    public class ChatMessageRepository : IChatMessageRepository
    {
        private readonly OnionDbContext _contex;

        public ChatMessageRepository(OnionDbContext contex)
        {
            _contex = contex;
        }

        public async Task<Responce> CreateAsync(ChatMessage chatMessage)
        {
            try
            {
                _contex.ChatMessages.Add(chatMessage);
                await _contex.SaveChangesAsync();
                return ResponseCreator.CreateSuccessifullResponse();
            }
            catch (Exception ex)
            {
                List<string> errorMeesages = new List<string>() { ex.Message };
                return ResponseCreator
                    .CreateUnsuccessifullResponse(errorMeesages,
                        (int)HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Responce> DeleteAsync(Guid id)
        {
            var chatMessage = await _contex.ChatMessages.FindAsync(id);
            try
            {
                _contex.Remove(chatMessage);
                await _contex.SaveChangesAsync();
                return ResponseCreator.CreateSuccessifullResponse();
            }
            catch (Exception ex)
            {
                List<string> errorMeesages = new List<string>() { ex.Message };
                return ResponseCreator
                    .CreateUnsuccessifullResponse(errorMeesages,
                        (int)HttpStatusCode.InternalServerError);
            }
        }


        public async Task<ChatMessage> FindByIdAsync(Guid id)
        {
            var chatMessage = await _contex.ChatMessages.FindAsync(id);
            return chatMessage;
        }

        public async Task<Responce> UpdateAsync(ChatMessage chatMessage)
        {
            try
            {
                _contex.ChatMessages.Update(chatMessage);
                await _contex.SaveChangesAsync();
                return ResponseCreator.CreateSuccessifullResponse();
            }
            catch (Exception ex)
            {
                List<string> errorMeesages = new List<string>() { ex.Message };
                return ResponseCreator
                    .CreateUnsuccessifullResponse(errorMeesages,
                        (int)HttpStatusCode.InternalServerError);
            }
        }

        public async Task<List<ChatMessage>> GetAllChatMessagesAsListAsync()
        {
            return await _contex.ChatMessages.ToListAsync();
        }

        public IQueryable<ChatMessage> GetAllChatMessages()
        {
            return _contex.ChatMessages;
        }
    }
}

