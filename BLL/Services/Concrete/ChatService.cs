using BLL.DTO;
using DAL.Helpers;
using BLL.Services.Abstract;
using DAL.Context;
using DAL.Domains;
using DAL.Repositories.Abstract;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using DinkToPdf.Contracts;
using DinkToPdf;
using BLL.Helpers.HtmlToPdfConverter;

namespace BLL.Services.Concrete
{
    public class ChatService : IChatService
    {
        private IConverter _converter;
        private readonly IMessageRepository _messageRepository;
        private readonly UserManager<User> _userManeger;
        private readonly IMapper _mapper;
        public ChatService(IMessageRepository messageRepository,
                           UserManager<User> userManeger,
                           OnionDbContext context,
                           IMapper mapper,
                           IConverter converter)
        {
            _messageRepository = messageRepository;
            _userManeger = userManeger;
            _mapper = mapper;
            _converter = converter;
        }


        public async Task<ResponceWithData<List<GroupedMessagesDTO>>> ReadAllSentMessagesAsync(string senderId)
        {
            var sendersWithMessagesDTO = await GetAllSentMessagesDTOs(senderId);
            return new ResponceWithData<List<GroupedMessagesDTO>>() { Data = sendersWithMessagesDTO, Success = true };
        }

        public async Task<ResponceWithData<List<GroupedMessagesDTO>>> ReadAllRecivedMessagesAsync(string reciverID)
        {
            var reciversWithMessagesDTO = await GetAllRecivedMessagesDTOs(reciverID);
            return new ResponceWithData<List<GroupedMessagesDTO>>() { Data = reciversWithMessagesDTO, Success = true };

        }

        public async Task<Responce> WriteMessageAsync(MessageWriteDTO messageDTO)
        {
            var sender = await _userManeger.FindByIdAsync(messageDTO.SenderId);
            var reciver = await _userManeger.FindByIdAsync(messageDTO.ReciverId);

            if (sender == null)
            {
                var errorMessage = new[] { "user with senderID was not found" }.ToList();
                return ResponseCreator.CreateUnsuccessifullResponse(errorMessage,
                               (int)HttpStatusCode.NotFound);
            }

            if (reciver == null)
            {
                var errorMessage = new[] { "user with reciverID was not found" }.ToList();
                return ResponseCreator.CreateUnsuccessifullResponse(errorMessage,
                               (int)HttpStatusCode.NotFound);
            }

            Message message = new Message
            {
                Sender = sender,
                Reciver = reciver,
                Body = messageDTO.Body
            };

            var response = await _messageRepository.CreateAsync(message);
            return response;
        }

        

        public async Task<Responce> DeleteMesageFromUserAsync(string messageStringId, string userStringId)
        {
            var userId = new Guid(userStringId);
            var message = await _messageRepository.FindByIdAsync(new Guid(messageStringId));

            if (message == null)
            {
                return ResponseCreator.CreateUnsuccessifullResponse(new[]
                    { "Message was not found" }.ToList()
                    , (int)HttpStatusCode.NotFound);
            }

            if (message.ReciverID == userId)
            {
                message.IsDeletedFromReciver = true;
            }
            else if (message.SenderID == userId)
            {
                message.IsDeletedFromSender = true;
            }
            else
            {
                return ResponseCreator.CreateUnsuccessifullResponse(new[]
                    {"Current user does not contain this message" }.ToList()
                    , (int)HttpStatusCode.NotFound);
            }

            await _messageRepository.UpdateAsync(message);
            return ResponseCreator.CreateSuccessifullResponse();
        }

        public async Task<Responce> DeleteMessageFromChatAsync(string messageId, string userStringId)
        {
            var userId = new Guid(userStringId);
            var message = await _messageRepository.FindByIdAsync(new Guid(messageId));

            if (message == null)
            {
                return ResponseCreator.CreateUnsuccessifullResponse(new[]
                    { "Message was not found" }.ToList()
                    , (int)HttpStatusCode.NotFound);
            }

            if (message.ReciverID != userId && message.SenderID != userId)
            {
                return ResponseCreator.CreateUnsuccessifullResponse(new[]
                   {"Current user does not contain this message" }.ToList()
                   , (int)HttpStatusCode.NotFound);
            }

            message.IsDeletedFromReciver = true;
            message.IsDeletedFromSender = true;
            await _messageRepository.UpdateAsync(message);
            return ResponseCreator.CreateSuccessifullResponse();
        }

        public async Task<Responce> UpdateMessage(string messageId)
        {
            var message = await _messageRepository.FindByIdAsync(new Guid(messageId));
            if (message == null)
            {
                return ResponseCreator.CreateUnsuccessifullResponse(new[]
                    { "Message was not found" }.ToList()
                    , (int)HttpStatusCode.NotFound);
            }
            await _messageRepository.UpdateAsync(message);
            return ResponseCreator.CreateSuccessifullResponse();
        }

        public async Task<Responce> ExportToPDF_AllRecivedMessagesAsync(string reciverId)
        {
            var groupedRecivedMessagesDTOs = await GetAllRecivedMessagesDTOs(reciverId);
            CreatePDF(groupedRecivedMessagesDTOs);
            return ResponseCreator.CreateSuccessifullResponse();
        }

        public async Task<Responce> ExportToPDF_AllSentMessages(string senderId)
        {
            var groupedSendMessagesDTOs = await GetAllSentMessagesDTOs(senderId);
            CreatePDF(groupedSendMessagesDTOs);
            return ResponseCreator.CreateSuccessifullResponse();
        }

        private async Task<List<GroupedMessagesDTO>> GetGrouppedMessagesDTOsAsync(IEnumerable<IGrouping<User, Message>> groupedMessagesDTOs)
        {
            List<GroupedMessagesDTO> usersWithMessagesDTO = new List<GroupedMessagesDTO>();
            foreach (var sender in groupedMessagesDTOs)
            {
                var userWithMessages = _mapper.Map<GroupedMessagesDTO>(sender.Key);
                userWithMessages.Messages = new List<MessageReadDTO>();
                foreach (var message in sender)
                {
                    message.ReadStatus = true;
                    await _messageRepository.UpdateAsync(message);
                    userWithMessages.Messages.Add(_mapper.Map<MessageReadDTO>(message));
                }
                usersWithMessagesDTO.Add(userWithMessages);
            }

            return usersWithMessagesDTO;
        }

        private async Task<List<GroupedMessagesDTO>> GetAllRecivedMessagesDTOs(string reciverID)
        {
            var messages = _messageRepository.GetAllMessages();
            var sendersWithMessages = messages
                .Include(m => m.Sender)?
                    .ThenInclude(c => c.Image)
                .Where(c => c.ReciverID == new Guid(reciverID))
                .Where(c => c.IsDeletedFromReciver == false)
                .AsEnumerable()
                .GroupBy(m => m.Sender);

            return await GetGrouppedMessagesDTOsAsync(sendersWithMessages);
        }

        private async Task<List<GroupedMessagesDTO>> GetAllSentMessagesDTOs(string senderID)
        {
            var messages = _messageRepository.GetAllMessages();
            var sendersWithMessages = messages
                .Include(m => m.Reciver)?
                    .ThenInclude(c => c.Image)
                .Where(c => c.SenderID == new Guid(senderID))
                .Where(c => c.IsDeletedFromSender == false)
                .AsEnumerable()
                .GroupBy(m => m.Reciver);

            return await GetGrouppedMessagesDTOsAsync(sendersWithMessages);
        }

        private void CreatePDF(List<GroupedMessagesDTO> groupedMessagesDTOs)
        {
            var uniqueId = Guid.NewGuid();
            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 10 },
                DocumentTitle = "PDF Report",
                Out = string.Format(@"E:\OmnioApp\MessagesPDF\MessageChat_Report{0}.pdf", uniqueId)
            };
            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                HtmlContent = TemplateGenerator.GetHTMLString(groupedMessagesDTOs),
                WebSettings = { DefaultEncoding = "utf-8" },
                HeaderSettings = { FontName = "Arial", FontSize = 9, Right = "Page [page] of [toPage]", Line = true },
                FooterSettings = { FontName = "Arial", FontSize = 9, Line = true, Center = "Report Footer" }
            };
            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };
            _converter.Convert(pdf);
        }

    }
}
