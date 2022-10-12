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
    public class ChatRepository : IChatRepository
    {
        private readonly OnionDbContext _contex;

        public ChatRepository(OnionDbContext contex)
        {
            _contex = contex;
        }

        public async Task<Responce> CreateAsync(Chat chat)
        {
            try
            {
                _contex.Chats.Add(chat);
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
            var chat = await _contex.Chats.FindAsync(id);
            try
            {
                _contex.Remove(chat);
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


        public async Task<Chat> FindByIdAsync(Guid id)
        {
            var chat = await _contex.Chats.FindAsync(id);
            return chat;
        }

        public async Task<Responce> UpdateAsync(Chat chat)
        {
            try
            {
                _contex.Chats.Update(chat);
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

        public async Task<List<Chat>> GetAllChatsAsListAsync()
        {
            return await _contex.Chats.ToListAsync();
        }

        public IQueryable<Chat> GetAllChats()
        {
            return _contex.Chats;
        }
    }
}
