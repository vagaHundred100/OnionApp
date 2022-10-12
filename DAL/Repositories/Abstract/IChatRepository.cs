using DAL.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories.Abstract
{
    public interface IChatRepository
    {
        Task<Responce> CreateAsync(Chat chat);
        Task<Responce> UpdateAsync(Chat chat);
        Task<Responce> DeleteAsync(Guid id);
        Task<Chat> FindByIdAsync(Guid id);
        Task<List<Chat>> GetAllChatsAsListAsync();
        IQueryable<Chat> GetAllChats();
    }
}
