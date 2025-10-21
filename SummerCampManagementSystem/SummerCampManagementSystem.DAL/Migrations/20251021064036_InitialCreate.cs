using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SummerCampManagementSystem.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccommodationType",
                columns: table => new
                {
                    accommodationTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Accommod__7380C37A36BF9C0D", x => x.accommodationTypeId);
                });

            migrationBuilder.CreateTable(
                name: "ActivitySchedule",
                columns: table => new
                {
                    activityScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    activityId = table.Column<int>(type: "int", nullable: false),
                    staffId = table.Column<int>(type: "int", nullable: true),
                    startTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    endTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isLivestream = table.Column<bool>(type: "bit", nullable: true),
                    roomId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    maxCapacity = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Activity__32136F49C26ADD1F", x => x.activityScheduleId);
                });

            migrationBuilder.CreateTable(
                name: "Badge",
                columns: table => new
                {
                    badgeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    badgeName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Badge__75B46C9F476C0756", x => x.badgeId);
                });

            migrationBuilder.CreateTable(
                name: "CamperRegistration",
                columns: table => new
                {
                    camperId = table.Column<int>(type: "int", nullable: false),
                    registrationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CamperRegistration", x => new { x.camperId, x.registrationId });
                });

            migrationBuilder.CreateTable(
                name: "CampType",
                columns: table => new
                {
                    campTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CampType__3EADA5F5A74792CC", x => x.campTypeId);
                });

            migrationBuilder.CreateTable(
                name: "ChatRoom",
                columns: table => new
                {
                    chatRoomId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ChatRoom__CB58B49253ED31B5", x => x.chatRoomId);
                });

            migrationBuilder.CreateTable(
                name: "FAQ",
                columns: table => new
                {
                    faqId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    question = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    answer = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__FAQ__B18E4567C2723C94", x => x.faqId);
                });

            migrationBuilder.CreateTable(
                name: "Guardian",
                columns: table => new
                {
                    guardianId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    gender = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    dob = table.Column<DateOnly>(type: "date", nullable: true),
                    answer = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Guardian__8A1718E14FC024F6", x => x.guardianId);
                });

            migrationBuilder.CreateTable(
                name: "PromotionType",
                columns: table => new
                {
                    promotionTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    updateAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Promotio__847F158D388561AD", x => x.promotionTypeId);
                });

            migrationBuilder.CreateTable(
                name: "UserAccount",
                columns: table => new
                {
                    userId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    firstName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    lastName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    phoneNumber = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    avatar = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    dob = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserAcco__CB9A1CFF87D326B1", x => x.userId);
                });

            migrationBuilder.CreateTable(
                name: "VehicleType",
                columns: table => new
                {
                    vehicleTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__VehicleT__4709A1D4F0649896", x => x.vehicleTypeId);
                });

            migrationBuilder.CreateTable(
                name: "BankUser",
                columns: table => new
                {
                    bankUserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<int>(type: "int", nullable: true),
                    bankCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    bankName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    bankNumber = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    isPrimary = table.Column<bool>(type: "bit", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BankUser__CBF11725229B75CF", x => x.bankUserId);
                    table.ForeignKey(
                        name: "FK__BankUser__userId__7D439ABD",
                        column: x => x.userId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "Blog",
                columns: table => new
                {
                    blogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    authorId = table.Column<int>(type: "int", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Blog__FA0AA72D8027A916", x => x.blogId);
                    table.ForeignKey(
                        name: "FK__Blog__authorId__0880433F",
                        column: x => x.authorId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "ChatMessageAI",
                columns: table => new
                {
                    chatMessageAiId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    senderId = table.Column<int>(type: "int", nullable: true),
                    message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    messageCreateAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    responseAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ChatMess__DBC8291A920D34D4", x => x.chatMessageAiId);
                    table.ForeignKey(
                        name: "FK__ChatMessa__sende__607251E5",
                        column: x => x.senderId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "ChatRoomUser",
                columns: table => new
                {
                    chatRoomUserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    chatRoomId = table.Column<int>(type: "int", nullable: true),
                    userId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ChatRoom__C9D3D5664196E8D8", x => x.chatRoomUserId);
                    table.ForeignKey(
                        name: "FK__ChatRoomU__chatR__57DD0BE4",
                        column: x => x.chatRoomId,
                        principalTable: "ChatRoom",
                        principalColumn: "chatRoomId");
                    table.ForeignKey(
                        name: "FK__ChatRoomU__userI__58D1301D",
                        column: x => x.userId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "Driver",
                columns: table => new
                {
                    driverId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<int>(type: "int", nullable: true),
                    driverName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    driverPhoneNumber = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    driverAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    driverLicense = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    dob = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Driver__F1532DF2E68365B1", x => x.driverId);
                    table.ForeignKey(
                        name: "FK__Driver__userId__02084FDA",
                        column: x => x.userId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "Livestream",
                columns: table => new
                {
                    livestreamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    roomId = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    hostId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Livestre__650D2CD3F56FAE02", x => x.livestreamId);
                    table.ForeignKey(
                        name: "FK__Livestrea__hostI__634EBE90",
                        column: x => x.hostId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "Message",
                columns: table => new
                {
                    messageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    chatRoomId = table.Column<int>(type: "int", nullable: true),
                    senderId = table.Column<int>(type: "int", nullable: true),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    receiverId = table.Column<int>(type: "int", nullable: true),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Message__4808B9934C3F9FF7", x => x.messageId);
                    table.ForeignKey(
                        name: "FK__Message__chatRoo__5BAD9CC8",
                        column: x => x.chatRoomId,
                        principalTable: "ChatRoom",
                        principalColumn: "chatRoomId");
                    table.ForeignKey(
                        name: "FK__Message__receive__5D95E53A",
                        column: x => x.receiverId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                    table.ForeignKey(
                        name: "FK__Message__senderI__5CA1C101",
                        column: x => x.senderId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    notificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    userId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Notifica__4BA5CEA9134CC0BA", x => x.notificationId);
                    table.ForeignKey(
                        name: "FK__Notificat__userI__7B264821",
                        column: x => x.userId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "Promotion",
                columns: table => new
                {
                    promotionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    startDate = table.Column<DateOnly>(type: "date", nullable: true),
                    endDate = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    percent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    maxDiscountAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    createBy = table.Column<int>(type: "int", nullable: true),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    promotionTypeId = table.Column<int>(type: "int", nullable: true),
                    code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Promotio__99EB696EC39A7ACC", x => x.promotionId);
                    table.ForeignKey(
                        name: "FK__Promotion__creat__0F624AF8",
                        column: x => x.createBy,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                    table.ForeignKey(
                        name: "FK__Promotion__promo__10566F31",
                        column: x => x.promotionTypeId,
                        principalTable: "PromotionType",
                        principalColumn: "promotionTypeId");
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<int>(type: "int", nullable: false),
                    token = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    expiresAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    isRevoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RefreshT__3213E83F646E62AB", x => x.id);
                    table.ForeignKey(
                        name: "FK_RefreshToken_User",
                        column: x => x.userId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "Vehicle",
                columns: table => new
                {
                    vehicleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vehicleType = table.Column<int>(type: "int", nullable: true),
                    vehicleName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    vehicleNumber = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    capacity = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Vehicle__5B9D25F2463A0876", x => x.vehicleId);
                    table.ForeignKey(
                        name: "FK__Vehicle__vehicle__4A8310C6",
                        column: x => x.vehicleType,
                        principalTable: "VehicleType",
                        principalColumn: "vehicleTypeId");
                });

            migrationBuilder.CreateTable(
                name: "LivestreamUser",
                columns: table => new
                {
                    livestreamUserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    livestreamId = table.Column<int>(type: "int", nullable: true),
                    userId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Livestre__A1E8B1E8EC9C9A7E", x => x.livestreamUserId);
                    table.ForeignKey(
                        name: "FK__Livestrea__lives__662B2B3B",
                        column: x => x.livestreamId,
                        principalTable: "Livestream",
                        principalColumn: "livestreamId");
                    table.ForeignKey(
                        name: "FK__Livestrea__userI__671F4F74",
                        column: x => x.userId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "DriverVehicle",
                columns: table => new
                {
                    driverVehicleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    driverId = table.Column<int>(type: "int", nullable: true),
                    vehicleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DriverVe__D1A5D3882F71F9B3", x => x.driverVehicleId);
                    table.ForeignKey(
                        name: "FK__DriverVeh__drive__4D5F7D71",
                        column: x => x.driverId,
                        principalTable: "Driver",
                        principalColumn: "driverId");
                    table.ForeignKey(
                        name: "FK__DriverVeh__vehic__4E53A1AA",
                        column: x => x.vehicleId,
                        principalTable: "Vehicle",
                        principalColumn: "vehicleId");
                });

            migrationBuilder.CreateTable(
                name: "Accommodation",
                columns: table => new
                {
                    accommodationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    campId = table.Column<int>(type: "int", nullable: true),
                    accommodationTypeId = table.Column<int>(type: "int", nullable: true),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    capacity = table.Column<int>(type: "int", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Accommod__20C0A5FD5401C017", x => x.accommodationId);
                    table.ForeignKey(
                        name: "FK__Accommoda__accom__1CBC4616",
                        column: x => x.accommodationTypeId,
                        principalTable: "AccommodationType",
                        principalColumn: "accommodationTypeId");
                });

            migrationBuilder.CreateTable(
                name: "Activity",
                columns: table => new
                {
                    activityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    activityType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    campId = table.Column<int>(type: "int", nullable: true),
                    locationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Activity__0FC9CBEC92704BD9", x => x.activityId);
                });

            migrationBuilder.CreateTable(
                name: "Album",
                columns: table => new
                {
                    albumId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    campId = table.Column<int>(type: "int", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: true),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Album__75BF3ECFA51B9673", x => x.albumId);
                });

            migrationBuilder.CreateTable(
                name: "AlbumPhoto",
                columns: table => new
                {
                    albumPhotoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    albumId = table.Column<int>(type: "int", nullable: true),
                    photo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    caption = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AlbumPho__695404581FC4A374", x => x.albumPhotoId);
                    table.ForeignKey(
                        name: "FK__AlbumPhot__album__00DF2177",
                        column: x => x.albumId,
                        principalTable: "Album",
                        principalColumn: "albumId");
                });

            migrationBuilder.CreateTable(
                name: "AlbumPhotoFace",
                columns: table => new
                {
                    albumPhotoFaceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    albumPhotoId = table.Column<int>(type: "int", nullable: false),
                    faceTemplate = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AlbumPho__CCCCD35F5649B2BA", x => x.albumPhotoFaceId);
                    table.ForeignKey(
                        name: "FK_AlbumPhotoFace_AlbumPhoto",
                        column: x => x.albumPhotoId,
                        principalTable: "AlbumPhoto",
                        principalColumn: "albumPhotoId");
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLog",
                columns: table => new
                {
                    attendanceLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    camperId = table.Column<int>(type: "int", nullable: false),
                    timestamp = table.Column<DateTime>(type: "datetime", nullable: false),
                    eventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    checkInMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    vehicleId = table.Column<int>(type: "int", nullable: true),
                    activityId = table.Column<int>(type: "int", nullable: true),
                    staffId = table.Column<int>(type: "int", nullable: true),
                    note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Attendan__F147FFAE82437F18", x => x.attendanceLogId);
                    table.ForeignKey(
                        name: "FK_AttendanceLog_Activity",
                        column: x => x.activityId,
                        principalTable: "Activity",
                        principalColumn: "activityId");
                    table.ForeignKey(
                        name: "FK_AttendanceLog_Staff",
                        column: x => x.staffId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                    table.ForeignKey(
                        name: "FK_AttendanceLog_Vehicle",
                        column: x => x.vehicleId,
                        principalTable: "Vehicle",
                        principalColumn: "vehicleId");
                });

            migrationBuilder.CreateTable(
                name: "Camp",
                columns: table => new
                {
                    campId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    place = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    minParticipants = table.Column<int>(type: "int", nullable: true),
                    maxParticipants = table.Column<int>(type: "int", nullable: true),
                    startDate = table.Column<DateOnly>(type: "date", nullable: true),
                    endDate = table.Column<DateOnly>(type: "date", nullable: true),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    campTypeId = table.Column<int>(type: "int", nullable: true),
                    image = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    createBy = table.Column<int>(type: "int", nullable: true),
                    locationId = table.Column<int>(type: "int", nullable: true),
                    promotionId = table.Column<int>(type: "int", nullable: true),
                    registrationStartDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    registrationEndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    minAge = table.Column<int>(type: "int", nullable: true),
                    maxAge = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Camp__BC586B191C6014DA", x => x.campId);
                    table.ForeignKey(
                        name: "FK_Camp_Promotion",
                        column: x => x.promotionId,
                        principalTable: "Promotion",
                        principalColumn: "promotionId");
                    table.ForeignKey(
                        name: "FK__Camp__campTypeId__04E4BC85",
                        column: x => x.campTypeId,
                        principalTable: "CampType",
                        principalColumn: "campTypeId");
                    table.ForeignKey(
                        name: "FK__Camp__createBy__05D8E0BE",
                        column: x => x.createBy,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "CampBadge",
                columns: table => new
                {
                    campBadgeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    badgeId = table.Column<int>(type: "int", nullable: true),
                    campId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CampBadg__7452AA76F8B74F8D", x => x.campBadgeId);
                    table.ForeignKey(
                        name: "FK__CampBadge__badge__208CD6FA",
                        column: x => x.badgeId,
                        principalTable: "Badge",
                        principalColumn: "badgeId");
                    table.ForeignKey(
                        name: "FK__CampBadge__campI__2180FB33",
                        column: x => x.campId,
                        principalTable: "Camp",
                        principalColumn: "campId");
                });

            migrationBuilder.CreateTable(
                name: "CamperGroup",
                columns: table => new
                {
                    camperGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    groupName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    maxSize = table.Column<int>(type: "int", nullable: true),
                    supervisorId = table.Column<int>(type: "int", nullable: true),
                    campId = table.Column<int>(type: "int", nullable: true),
                    minAge = table.Column<int>(type: "int", nullable: true),
                    maxAge = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CamperGr__A3F9F2EB18FD70C7", x => x.camperGroupId);
                    table.ForeignKey(
                        name: "FK__CamperGro__campI__18EBB532",
                        column: x => x.campId,
                        principalTable: "Camp",
                        principalColumn: "campId");
                    table.ForeignKey(
                        name: "FK__CamperGro__super__17F790F9",
                        column: x => x.supervisorId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "CampStaffAssignment",
                columns: table => new
                {
                    campStaffAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    staffId = table.Column<int>(type: "int", nullable: true),
                    campId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ManagerA__703B219833FB8987", x => x.campStaffAssignmentId);
                    table.ForeignKey(
                        name: "FK__ManagerAs__campI__151B244E",
                        column: x => x.campId,
                        principalTable: "Camp",
                        principalColumn: "campId");
                    table.ForeignKey(
                        name: "FK__ManagerAs__manag__14270015",
                        column: x => x.staffId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "DriverSchedule",
                columns: table => new
                {
                    driverScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    driverId = table.Column<int>(type: "int", nullable: true),
                    vehicleId = table.Column<int>(type: "int", nullable: true),
                    campId = table.Column<int>(type: "int", nullable: true),
                    workDate = table.Column<DateOnly>(type: "date", nullable: true),
                    startTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    endTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DriverSc__8ACADD5F9F179836", x => x.driverScheduleId);
                    table.ForeignKey(
                        name: "FK__DriverSch__campI__531856C7",
                        column: x => x.campId,
                        principalTable: "Camp",
                        principalColumn: "campId");
                    table.ForeignKey(
                        name: "FK__DriverSch__drive__51300E55",
                        column: x => x.driverId,
                        principalTable: "Driver",
                        principalColumn: "driverId");
                    table.ForeignKey(
                        name: "FK__DriverSch__vehic__5224328E",
                        column: x => x.vehicleId,
                        principalTable: "Vehicle",
                        principalColumn: "vehicleId");
                });

            migrationBuilder.CreateTable(
                name: "Registration",
                columns: table => new
                {
                    registrationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    campId = table.Column<int>(type: "int", nullable: true),
                    registrationCreateAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    appliedPromotionId = table.Column<int>(type: "int", nullable: true),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    userId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Registra__A3DB1435EB987530", x => x.registrationId);
                    table.ForeignKey(
                        name: "FK_Registration_AppliedPromotion",
                        column: x => x.appliedPromotionId,
                        principalTable: "Promotion",
                        principalColumn: "promotionId");
                    table.ForeignKey(
                        name: "FK_Registration_UserAccount",
                        column: x => x.userId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                    table.ForeignKey(
                        name: "FK__Registrat__campI__6CD828CA",
                        column: x => x.campId,
                        principalTable: "Camp",
                        principalColumn: "campId");
                });

            migrationBuilder.CreateTable(
                name: "Route",
                columns: table => new
                {
                    routeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    campId = table.Column<int>(type: "int", nullable: true),
                    routeName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Route__BAC024C756723709", x => x.routeId);
                    table.ForeignKey(
                        name: "FK__Route__campId__08B54D69",
                        column: x => x.campId,
                        principalTable: "Camp",
                        principalColumn: "campId");
                });

            migrationBuilder.CreateTable(
                name: "Camper",
                columns: table => new
                {
                    camperId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    camperName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    gender = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    groupId = table.Column<int>(type: "int", nullable: true),
                    dob = table.Column<DateOnly>(type: "date", nullable: true),
                    faceTemplate = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    avatar = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Camper__1F5EA63223697F05", x => x.camperId);
                    table.ForeignKey(
                        name: "FK__Camper__groupId__245D67DE",
                        column: x => x.groupId,
                        principalTable: "CamperGroup",
                        principalColumn: "camperGroupId");
                });

            migrationBuilder.CreateTable(
                name: "GroupActivity",
                columns: table => new
                {
                    groupActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    camperGroupId = table.Column<int>(type: "int", nullable: true),
                    activityId = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__GroupAct__4CEA4BBCEF38C6BA", x => x.groupActivityId);
                    table.ForeignKey(
                        name: "FK__GroupActi__activ__3E1D39E1",
                        column: x => x.activityId,
                        principalTable: "Activity",
                        principalColumn: "activityId");
                    table.ForeignKey(
                        name: "FK__GroupActi__campe__3D2915A8",
                        column: x => x.camperGroupId,
                        principalTable: "CamperGroup",
                        principalColumn: "camperGroupId");
                });

            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    feedbackId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    registrationId = table.Column<int>(type: "int", nullable: true),
                    userId = table.Column<int>(type: "int", nullable: true),
                    rating = table.Column<int>(type: "int", nullable: true),
                    comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    campId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Feedback__2613FD244309CC64", x => x.feedbackId);
                    table.ForeignKey(
                        name: "FK__Feedback__campId__7849DB76",
                        column: x => x.campId,
                        principalTable: "Camp",
                        principalColumn: "campId");
                    table.ForeignKey(
                        name: "FK__Feedback__regist__76619304",
                        column: x => x.registrationId,
                        principalTable: "Registration",
                        principalColumn: "registrationId");
                    table.ForeignKey(
                        name: "FK__Feedback__userId__7755B73D",
                        column: x => x.userId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "RegistrationCancel",
                columns: table => new
                {
                    registrationCancelId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    registrationId = table.Column<int>(type: "int", nullable: true),
                    reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    imageRefund = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Registra__1BFD300A16D0BB46", x => x.registrationCancelId);
                    table.ForeignKey(
                        name: "FK__Registrat__regis__73852659",
                        column: x => x.registrationId,
                        principalTable: "Registration",
                        principalColumn: "registrationId");
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    transactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    registrationId = table.Column<int>(type: "int", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    transactionTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    transactionCode = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Transact__9B57CF7216BC3CDE", x => x.transactionId);
                    table.ForeignKey(
                        name: "FK__Transacti__regis__70A8B9AE",
                        column: x => x.registrationId,
                        principalTable: "Registration",
                        principalColumn: "registrationId");
                });

            migrationBuilder.CreateTable(
                name: "Location",
                columns: table => new
                {
                    locationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    routeId = table.Column<int>(type: "int", nullable: true),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    locationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Location__30646B6E1B427B16", x => x.locationId);
                    table.ForeignKey(
                        name: "FK__Location__routeI__0B91BA14",
                        column: x => x.routeId,
                        principalTable: "Route",
                        principalColumn: "routeId");
                });

            migrationBuilder.CreateTable(
                name: "VehicleSchedule",
                columns: table => new
                {
                    vehicleScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vehicleId = table.Column<int>(type: "int", nullable: true),
                    routeId = table.Column<int>(type: "int", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__VehicleS__98B042A10CA86140", x => x.vehicleScheduleId);
                    table.ForeignKey(
                        name: "FK__VehicleSc__route__0C50D423",
                        column: x => x.routeId,
                        principalTable: "Route",
                        principalColumn: "routeId");
                    table.ForeignKey(
                        name: "FK__VehicleSc__vehic__0B5CAFEA",
                        column: x => x.vehicleId,
                        principalTable: "Vehicle",
                        principalColumn: "vehicleId");
                });

            migrationBuilder.CreateTable(
                name: "CamperAccommodation",
                columns: table => new
                {
                    camperAccommodationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    camperId = table.Column<int>(type: "int", nullable: false),
                    accommodationId = table.Column<int>(type: "int", nullable: false),
                    assignedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CamperAc__3784380FF16C0712", x => x.camperAccommodationId);
                    table.ForeignKey(
                        name: "FK_CamperAccommodation_Accommodation",
                        column: x => x.accommodationId,
                        principalTable: "Accommodation",
                        principalColumn: "accommodationId");
                    table.ForeignKey(
                        name: "FK_CamperAccommodation_Camper",
                        column: x => x.camperId,
                        principalTable: "Camper",
                        principalColumn: "camperId");
                });

            migrationBuilder.CreateTable(
                name: "CamperActivity",
                columns: table => new
                {
                    camperActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    camperId = table.Column<int>(type: "int", nullable: true),
                    activityId = table.Column<int>(type: "int", nullable: true),
                    participationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CamperAc__B77C246F3CB74DB4", x => x.camperActivityId);
                    table.ForeignKey(
                        name: "FK__CamperAct__activ__3A4CA8FD",
                        column: x => x.activityId,
                        principalTable: "Activity",
                        principalColumn: "activityId");
                    table.ForeignKey(
                        name: "FK__CamperAct__campe__395884C4",
                        column: x => x.camperId,
                        principalTable: "Camper",
                        principalColumn: "camperId");
                });

            migrationBuilder.CreateTable(
                name: "CamperBadge",
                columns: table => new
                {
                    camperBadgeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    camperId = table.Column<int>(type: "int", nullable: true),
                    badgeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CamperBa__67972E47D42C121E", x => x.camperBadgeId);
                    table.ForeignKey(
                        name: "FK__CamperBad__badge__32AB8735",
                        column: x => x.badgeId,
                        principalTable: "Badge",
                        principalColumn: "badgeId");
                    table.ForeignKey(
                        name: "FK__CamperBad__campe__31B762FC",
                        column: x => x.camperId,
                        principalTable: "Camper",
                        principalColumn: "camperId");
                });

            migrationBuilder.CreateTable(
                name: "CamperGuardian",
                columns: table => new
                {
                    camperGuardianId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    camperId = table.Column<int>(type: "int", nullable: true),
                    guardianId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CamperGu__B304497C5424285B", x => x.camperGuardianId);
                    table.ForeignKey(
                        name: "FK__CamperGua__campe__2739D489",
                        column: x => x.camperId,
                        principalTable: "Camper",
                        principalColumn: "camperId");
                    table.ForeignKey(
                        name: "FK__CamperGua__guard__282DF8C2",
                        column: x => x.guardianId,
                        principalTable: "Guardian",
                        principalColumn: "guardianId");
                });

            migrationBuilder.CreateTable(
                name: "HealthRecord",
                columns: table => new
                {
                    healthRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    condition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    allergies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isAllergy = table.Column<bool>(type: "bit", nullable: true),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    camperId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__HealthRe__59B2D406F4033F3E", x => x.healthRecordId);
                    table.ForeignKey(
                        name: "FK__HealthRec__campe__2EDAF651",
                        column: x => x.camperId,
                        principalTable: "Camper",
                        principalColumn: "camperId");
                });

            migrationBuilder.CreateTable(
                name: "Incident",
                columns: table => new
                {
                    incidentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    incidentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    camperId = table.Column<int>(type: "int", nullable: true),
                    campStaffId = table.Column<int>(type: "int", nullable: true),
                    campId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Incident__06A5D741842EED80", x => x.incidentId);
                    table.ForeignKey(
                        name: "FK__Incident__campId__47A6A41B",
                        column: x => x.campId,
                        principalTable: "Camp",
                        principalColumn: "campId");
                    table.ForeignKey(
                        name: "FK__Incident__campSt__46B27FE2",
                        column: x => x.campStaffId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                    table.ForeignKey(
                        name: "FK__Incident__camper__45BE5BA9",
                        column: x => x.camperId,
                        principalTable: "Camper",
                        principalColumn: "camperId");
                });

            migrationBuilder.CreateTable(
                name: "ParentCamper",
                columns: table => new
                {
                    parentCamperId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parentId = table.Column<int>(type: "int", nullable: true),
                    camperId = table.Column<int>(type: "int", nullable: true),
                    relationship = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ParentCa__6645EA93BF9107B7", x => x.parentCamperId);
                    table.ForeignKey(
                        name: "FK__ParentCam__campe__2BFE89A6",
                        column: x => x.camperId,
                        principalTable: "Camper",
                        principalColumn: "camperId");
                    table.ForeignKey(
                        name: "FK__ParentCam__paren__2B0A656D",
                        column: x => x.parentId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "RegistrationCamper",
                columns: table => new
                {
                    registrationId = table.Column<int>(type: "int", nullable: false),
                    camperId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Registra__922EFE5614C859DF", x => new { x.registrationId, x.camperId });
                    table.ForeignKey(
                        name: "FK__Registrat__campe__308E3499",
                        column: x => x.camperId,
                        principalTable: "Camper",
                        principalColumn: "camperId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__Registrat__regis__2F9A1060",
                        column: x => x.registrationId,
                        principalTable: "Registration",
                        principalColumn: "registrationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Report",
                columns: table => new
                {
                    reportId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    camperId = table.Column<int>(type: "int", nullable: true),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    image = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    reportedBy = table.Column<int>(type: "int", nullable: true),
                    activityId = table.Column<int>(type: "int", nullable: true),
                    level = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Report__1C9B4E2D17DAFC9F", x => x.reportId);
                    table.ForeignKey(
                        name: "FK__Report__activity__42E1EEFE",
                        column: x => x.activityId,
                        principalTable: "Activity",
                        principalColumn: "activityId");
                    table.ForeignKey(
                        name: "FK__Report__camperId__40F9A68C",
                        column: x => x.camperId,
                        principalTable: "Camper",
                        principalColumn: "camperId");
                    table.ForeignKey(
                        name: "FK__Report__reported__41EDCAC5",
                        column: x => x.reportedBy,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "Visitation",
                columns: table => new
                {
                    visitationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userId = table.Column<int>(type: "int", nullable: true),
                    camperId = table.Column<int>(type: "int", nullable: true),
                    visitStartTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    visitEndTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    approvedBy = table.Column<int>(type: "int", nullable: true),
                    CheckInTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    CheckOutTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    createAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    updateAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Visitati__E33BAE4142B08E97", x => x.visitationId);
                    table.ForeignKey(
                        name: "FK__Visitatio__appro__05A3D694",
                        column: x => x.approvedBy,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                    table.ForeignKey(
                        name: "FK__Visitatio__campe__04AFB25B",
                        column: x => x.camperId,
                        principalTable: "Camper",
                        principalColumn: "camperId");
                    table.ForeignKey(
                        name: "FK__Visitatio__userI__03BB8E22",
                        column: x => x.userId,
                        principalTable: "UserAccount",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accommodation_accommodationTypeId",
                table: "Accommodation",
                column: "accommodationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Accommodation_campId",
                table: "Accommodation",
                column: "campId");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_campId",
                table: "Activity",
                column: "campId");

            migrationBuilder.CreateIndex(
                name: "IX_Activity_locationId",
                table: "Activity",
                column: "locationId");

            migrationBuilder.CreateIndex(
                name: "IX_Album_campId",
                table: "Album",
                column: "campId");

            migrationBuilder.CreateIndex(
                name: "IX_AlbumPhoto_albumId",
                table: "AlbumPhoto",
                column: "albumId");

            migrationBuilder.CreateIndex(
                name: "IX_AlbumPhotoFace_albumPhotoId",
                table: "AlbumPhotoFace",
                column: "albumPhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLog_activityId",
                table: "AttendanceLog",
                column: "activityId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLog_camperId",
                table: "AttendanceLog",
                column: "camperId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLog_staffId",
                table: "AttendanceLog",
                column: "staffId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLog_vehicleId",
                table: "AttendanceLog",
                column: "vehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_BankUser_userId",
                table: "BankUser",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_authorId",
                table: "Blog",
                column: "authorId");

            migrationBuilder.CreateIndex(
                name: "IX_Camp_campTypeId",
                table: "Camp",
                column: "campTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Camp_createBy",
                table: "Camp",
                column: "createBy");

            migrationBuilder.CreateIndex(
                name: "IX_Camp_locationId",
                table: "Camp",
                column: "locationId");

            migrationBuilder.CreateIndex(
                name: "IX_Camp_promotionId",
                table: "Camp",
                column: "promotionId");

            migrationBuilder.CreateIndex(
                name: "IX_CampBadge_badgeId",
                table: "CampBadge",
                column: "badgeId");

            migrationBuilder.CreateIndex(
                name: "IX_CampBadge_campId",
                table: "CampBadge",
                column: "campId");

            migrationBuilder.CreateIndex(
                name: "IX_Camper_groupId",
                table: "Camper",
                column: "groupId");

            migrationBuilder.CreateIndex(
                name: "IX_CamperAccommodation_accommodationId",
                table: "CamperAccommodation",
                column: "accommodationId");

            migrationBuilder.CreateIndex(
                name: "IX_CamperAccommodation_camperId",
                table: "CamperAccommodation",
                column: "camperId");

            migrationBuilder.CreateIndex(
                name: "IX_CamperActivity_activityId",
                table: "CamperActivity",
                column: "activityId");

            migrationBuilder.CreateIndex(
                name: "IX_CamperActivity_camperId",
                table: "CamperActivity",
                column: "camperId");

            migrationBuilder.CreateIndex(
                name: "IX_CamperBadge_badgeId",
                table: "CamperBadge",
                column: "badgeId");

            migrationBuilder.CreateIndex(
                name: "IX_CamperBadge_camperId",
                table: "CamperBadge",
                column: "camperId");

            migrationBuilder.CreateIndex(
                name: "IX_CamperGroup_campId",
                table: "CamperGroup",
                column: "campId");

            migrationBuilder.CreateIndex(
                name: "IX_CamperGroup_supervisorId",
                table: "CamperGroup",
                column: "supervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_CamperGuardian_camperId",
                table: "CamperGuardian",
                column: "camperId");

            migrationBuilder.CreateIndex(
                name: "IX_CamperGuardian_guardianId",
                table: "CamperGuardian",
                column: "guardianId");

            migrationBuilder.CreateIndex(
                name: "IX_CampStaffAssignment_campId",
                table: "CampStaffAssignment",
                column: "campId");

            migrationBuilder.CreateIndex(
                name: "IX_CampStaffAssignment_staffId",
                table: "CampStaffAssignment",
                column: "staffId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageAI_senderId",
                table: "ChatMessageAI",
                column: "senderId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRoomUser_chatRoomId",
                table: "ChatRoomUser",
                column: "chatRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRoomUser_userId",
                table: "ChatRoomUser",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_Driver_userId",
                table: "Driver",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverSchedule_campId",
                table: "DriverSchedule",
                column: "campId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverSchedule_driverId",
                table: "DriverSchedule",
                column: "driverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverSchedule_vehicleId",
                table: "DriverSchedule",
                column: "vehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverVehicle_driverId",
                table: "DriverVehicle",
                column: "driverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverVehicle_vehicleId",
                table: "DriverVehicle",
                column: "vehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_campId",
                table: "Feedback",
                column: "campId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_registrationId",
                table: "Feedback",
                column: "registrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_userId",
                table: "Feedback",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupActivity_activityId",
                table: "GroupActivity",
                column: "activityId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupActivity_camperGroupId",
                table: "GroupActivity",
                column: "camperGroupId");

            migrationBuilder.CreateIndex(
                name: "UQ_HealthRecord_CamperId",
                table: "HealthRecord",
                column: "camperId",
                unique: true,
                filter: "[camperId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_camperId",
                table: "Incident",
                column: "camperId");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_campId",
                table: "Incident",
                column: "campId");

            migrationBuilder.CreateIndex(
                name: "IX_Incident_campStaffId",
                table: "Incident",
                column: "campStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Livestream_hostId",
                table: "Livestream",
                column: "hostId");

            migrationBuilder.CreateIndex(
                name: "IX_LivestreamUser_livestreamId",
                table: "LivestreamUser",
                column: "livestreamId");

            migrationBuilder.CreateIndex(
                name: "IX_LivestreamUser_userId",
                table: "LivestreamUser",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_Location_routeId",
                table: "Location",
                column: "routeId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_chatRoomId",
                table: "Message",
                column: "chatRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_receiverId",
                table: "Message",
                column: "receiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_senderId",
                table: "Message",
                column: "senderId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_userId",
                table: "Notifications",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentCamper_camperId",
                table: "ParentCamper",
                column: "camperId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentCamper_parentId",
                table: "ParentCamper",
                column: "parentId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotion_createBy",
                table: "Promotion",
                column: "createBy");

            migrationBuilder.CreateIndex(
                name: "IX_Promotion_promotionTypeId",
                table: "Promotion",
                column: "promotionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_userId",
                table: "RefreshToken",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_Registration_appliedPromotionId",
                table: "Registration",
                column: "appliedPromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_Registration_campId",
                table: "Registration",
                column: "campId");

            migrationBuilder.CreateIndex(
                name: "IX_Registration_userId",
                table: "Registration",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationCamper_camperId",
                table: "RegistrationCamper",
                column: "camperId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationCancel_registrationId",
                table: "RegistrationCancel",
                column: "registrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Report_activityId",
                table: "Report",
                column: "activityId");

            migrationBuilder.CreateIndex(
                name: "IX_Report_camperId",
                table: "Report",
                column: "camperId");

            migrationBuilder.CreateIndex(
                name: "IX_Report_reportedBy",
                table: "Report",
                column: "reportedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Route_campId",
                table: "Route",
                column: "campId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_registrationId",
                table: "Transaction",
                column: "registrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_vehicleType",
                table: "Vehicle",
                column: "vehicleType");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSchedule_routeId",
                table: "VehicleSchedule",
                column: "routeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSchedule_vehicleId",
                table: "VehicleSchedule",
                column: "vehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitation_approvedBy",
                table: "Visitation",
                column: "approvedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Visitation_camperId",
                table: "Visitation",
                column: "camperId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitation_userId",
                table: "Visitation",
                column: "userId");

            migrationBuilder.AddForeignKey(
                name: "FK__Accommoda__campI__1BC821DD",
                table: "Accommodation",
                column: "campId",
                principalTable: "Camp",
                principalColumn: "campId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activity_Camp",
                table: "Activity",
                column: "campId",
                principalTable: "Camp",
                principalColumn: "campId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activity_Location",
                table: "Activity",
                column: "locationId",
                principalTable: "Location",
                principalColumn: "locationId");

            migrationBuilder.AddForeignKey(
                name: "FK__Album__campId__7E02B4CC",
                table: "Album",
                column: "campId",
                principalTable: "Camp",
                principalColumn: "campId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceLog_Camper",
                table: "AttendanceLog",
                column: "camperId",
                principalTable: "Camper",
                principalColumn: "camperId");

            migrationBuilder.AddForeignKey(
                name: "FK_Camp_Location",
                table: "Camp",
                column: "locationId",
                principalTable: "Location",
                principalColumn: "locationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Route__campId__08B54D69",
                table: "Route");

            migrationBuilder.DropTable(
                name: "ActivitySchedule");

            migrationBuilder.DropTable(
                name: "AlbumPhotoFace");

            migrationBuilder.DropTable(
                name: "AttendanceLog");

            migrationBuilder.DropTable(
                name: "BankUser");

            migrationBuilder.DropTable(
                name: "Blog");

            migrationBuilder.DropTable(
                name: "CampBadge");

            migrationBuilder.DropTable(
                name: "CamperAccommodation");

            migrationBuilder.DropTable(
                name: "CamperActivity");

            migrationBuilder.DropTable(
                name: "CamperBadge");

            migrationBuilder.DropTable(
                name: "CamperGuardian");

            migrationBuilder.DropTable(
                name: "CamperRegistration");

            migrationBuilder.DropTable(
                name: "CampStaffAssignment");

            migrationBuilder.DropTable(
                name: "ChatMessageAI");

            migrationBuilder.DropTable(
                name: "ChatRoomUser");

            migrationBuilder.DropTable(
                name: "DriverSchedule");

            migrationBuilder.DropTable(
                name: "DriverVehicle");

            migrationBuilder.DropTable(
                name: "FAQ");

            migrationBuilder.DropTable(
                name: "Feedback");

            migrationBuilder.DropTable(
                name: "GroupActivity");

            migrationBuilder.DropTable(
                name: "HealthRecord");

            migrationBuilder.DropTable(
                name: "Incident");

            migrationBuilder.DropTable(
                name: "LivestreamUser");

            migrationBuilder.DropTable(
                name: "Message");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ParentCamper");

            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropTable(
                name: "RegistrationCamper");

            migrationBuilder.DropTable(
                name: "RegistrationCancel");

            migrationBuilder.DropTable(
                name: "Report");

            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropTable(
                name: "VehicleSchedule");

            migrationBuilder.DropTable(
                name: "Visitation");

            migrationBuilder.DropTable(
                name: "AlbumPhoto");

            migrationBuilder.DropTable(
                name: "Accommodation");

            migrationBuilder.DropTable(
                name: "Badge");

            migrationBuilder.DropTable(
                name: "Guardian");

            migrationBuilder.DropTable(
                name: "Driver");

            migrationBuilder.DropTable(
                name: "Livestream");

            migrationBuilder.DropTable(
                name: "ChatRoom");

            migrationBuilder.DropTable(
                name: "Activity");

            migrationBuilder.DropTable(
                name: "Registration");

            migrationBuilder.DropTable(
                name: "Vehicle");

            migrationBuilder.DropTable(
                name: "Camper");

            migrationBuilder.DropTable(
                name: "Album");

            migrationBuilder.DropTable(
                name: "AccommodationType");

            migrationBuilder.DropTable(
                name: "VehicleType");

            migrationBuilder.DropTable(
                name: "CamperGroup");

            migrationBuilder.DropTable(
                name: "Camp");

            migrationBuilder.DropTable(
                name: "Location");

            migrationBuilder.DropTable(
                name: "Promotion");

            migrationBuilder.DropTable(
                name: "CampType");

            migrationBuilder.DropTable(
                name: "Route");

            migrationBuilder.DropTable(
                name: "UserAccount");

            migrationBuilder.DropTable(
                name: "PromotionType");
        }
    }
}
