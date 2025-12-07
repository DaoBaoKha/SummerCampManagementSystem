using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SummerCampManagementSystem.BLL.DTOs.ChatRoom;
using SummerCampManagementSystem.BLL.Exceptions;
using SummerCampManagementSystem.BLL.Interfaces;
using SummerCampManagementSystem.DAL.Models;
using SummerCampManagementSystem.DAL.UnitOfWork;

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
            // validation
            await ValidateSendMessageRequest(userId, request.ChatRoomId);

            // map DTO -> Entity 
            var message = _mapper.Map<Message>(request);
            message.senderId = userId; // get userId from token

            await _unitOfWork.Messages.CreateAsync(message);
            await _unitOfWork.CommitAsync();

            // get user info
            var sender = await _unitOfWork.Users.GetByIdAsync(userId);
            if (sender == null) throw new NotFoundException("Không tìm thấy thông tin người gửi.");

            message.sender = sender; 

            // map Entity -> Response DTO
            var responseDto = _mapper.Map<ChatRoomMessageDto>(message);

            // real-time
            await _chatNotifier.SendMessageToGroupAsync(request.ChatRoomId.ToString(), responseDto);

            return responseDto;
        }

        public async Task<IEnumerable<ChatRoomDetailDto>> GetMyChatRoomsAsync(int userId)
        {
            // get rooms of user
            var rooms = await _unitOfWork.ChatRoomUsers.GetRoomsByUserIdAsync(userId);

            var result = new List<ChatRoomDetailDto>();

            // logic to determine display name and avatar
            foreach (var room in rooms)
            {
                var lastMsg = room.Messages.FirstOrDefault();

                string displayName = room.name;
                string displayAvatar = "";
                int type = 1; // group

                // if private chat or no name taken, use other user's name
                if (string.IsNullOrEmpty(displayName) || room.ChatRoomUsers.Count == 2)
                {
                    type = 0; // private
                    var otherUser = room.ChatRoomUsers.FirstOrDefault(u => u.userId != userId)?.user;
                    if (otherUser != null)
                    {
                        displayName = $"{otherUser.lastName} {otherUser.firstName}";
                        displayAvatar = otherUser.avatar;
                    }
                    else
                    {
                        displayName = "Người dùng";
                    }
                }

                result.Add(new ChatRoomDetailDto
                {
                    ChatRoomId = room.chatRoomId,
                    Name = displayName,
                    AvatarUrl = displayAvatar,
                    LastMessage = lastMsg?.content ?? "Chưa có tin nhắn",
                    LastMessageTime = lastMsg?.createAt,
                    Type = type
                });
            }

            // get latest message on top
            return result.OrderByDescending(r => r.LastMessageTime).ToList();
        }

        public async Task<IEnumerable<ChatRoomMessageDto>> GetMessagesByRoomIdAsync(int userId, int chatRoomId)
        {
            // check if user in room
            var isInRoom = await _unitOfWork.ChatRoomUsers.IsUserInRoomAsync(userId, chatRoomId);
            if (!isInRoom)
                throw new UnauthorizedException("Bạn không có quyền xem tin nhắn của phòng này.");

            // get messages in room
            var messages = await _unitOfWork.Messages.GetMessagesByRoomIdAsync(chatRoomId);

            return _mapper.Map<IEnumerable<ChatRoomMessageDto>>(messages);
        }

        #region Private Methods

        private async Task ValidateSendMessageRequest(int userId, int chatRoomId)
        {
            // check if room exist
            var roomExists = await _unitOfWork.ChatRooms.ExistsAsync(chatRoomId);
            if (!roomExists)
            {
                throw new NotFoundException($"Không tìm thấy phòng chat với ID {chatRoomId}.");
            }

            // check if user in room
            var isInRoom = await _unitOfWork.ChatRoomUsers.IsUserInRoomAsync(userId, chatRoomId);
            if (!isInRoom)
            {
                throw new UnauthorizedException("Bạn không phải thành viên của phòng chat này nên không thể gửi tin nhắn.");
            }
        }

        #endregion
    }
}