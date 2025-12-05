using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.ChatRoom;
using SummerCampManagementSystem.BLL.Exceptions; // Import Custom Exception
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;
using System;
using System.Threading.Tasks;

namespace SummerCampManagementSystem.BLL.Services
{
    public class ChatRoomService : IChatRoomService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IChatNotifier _chatNotifier;

        public ChatRoomService(IUnitOfWork unitOfWork, IMapper mapper, IChatNotifier chatNotifier)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _chatNotifier = chatNotifier;
        }

        public async Task<ChatRoomMessageDto> SendMessageAsync(int userId, SendMessageDto request)
        {
            // check if room exist
            var roomExists = await _unitOfWork.ChatRooms.GetQueryable()
                .AnyAsync(r => r.chatRoomId == request.ChatRoomId);

            if (!roomExists)
            {
                throw new NotFoundException($"Không tìm thấy phòng chat với ID {request.ChatRoomId}.");
            }

            // check if user belongs to this room
            var isInRoom = await _unitOfWork.ChatRoomUsers.GetQueryable()
                .AnyAsync(u => u.chatRoomId == request.ChatRoomId && u.userId == userId);

            if (!isInRoom)
            {
                throw new UnauthorizedException("Bạn không phải thành viên của phòng chat này nên không thể gửi tin nhắn.");
            }

            // map DTO -> Entity
            var message = _mapper.Map<Message>(request);
            message.senderId = userId;

            // time always != null
            if (message.createAt == null) message.createAt = DateTime.UtcNow;

            await _unitOfWork.Messages.CreateAsync(message);

            // TODO:
            // update last interact time (Optional - for sort list chat)
            /* var room = await _unitOfWork.ChatRooms.GetByIdAsync(request.ChatRoomId);
            if (room != null) {
                room.updatedAt = DateTime.UtcNow;
                await _unitOfWork.ChatRooms.UpdateAsync(room);
            }
            */

            await _unitOfWork.CommitAsync();

            // get user info
            var sender = await _unitOfWork.Users.GetByIdAsync(userId);
            if (sender == null)
            {
                throw new NotFoundException($"Không tìm thấy thông tin người dùng với ID {userId}.");
            }

            // sender into message to get user info
            message.sender = sender;

            // map Entity -> DTO
            var responseDto = _mapper.Map<ChatRoomMessageDto>(message);

            // real-time
            await _chatNotifier.SendMessageToGroupAsync(request.ChatRoomId.ToString(), responseDto);

            return responseDto;
        }
    }
}