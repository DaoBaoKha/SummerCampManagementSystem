IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AccommodationType] (
    [accommodationTypeId] int NOT NULL IDENTITY,
    [name] nvarchar(255) NULL,
    [description] nvarchar(max) NULL,
    [isActive] bit NULL,
    CONSTRAINT [PK__Accommod__7380C37A36BF9C0D] PRIMARY KEY ([accommodationTypeId])
);
GO

CREATE TABLE [ActivitySchedule] (
    [activityScheduleId] int NOT NULL IDENTITY,
    [activityId] int NOT NULL,
    [staffId] int NULL,
    [startTime] datetime NULL,
    [endTime] datetime NULL,
    [status] nvarchar(50) NULL,
    [isLivestream] bit NULL,
    [roomId] varchar(255) NULL,
    [maxCapacity] int NULL,
    CONSTRAINT [PK__Activity__32136F49C26ADD1F] PRIMARY KEY ([activityScheduleId])
);
GO

CREATE TABLE [Badge] (
    [badgeId] int NOT NULL IDENTITY,
    [badgeName] nvarchar(255) NULL,
    [status] nvarchar(50) NULL,
    CONSTRAINT [PK__Badge__75B46C9F476C0756] PRIMARY KEY ([badgeId])
);
GO

CREATE TABLE [CamperRegistration] (
    [camperId] int NOT NULL,
    [registrationId] int NOT NULL,
    CONSTRAINT [PK_CamperRegistration] PRIMARY KEY ([camperId], [registrationId])
);
GO

CREATE TABLE [CampType] (
    [campTypeId] int NOT NULL IDENTITY,
    [name] nvarchar(255) NULL,
    [description] nvarchar(max) NULL,
    [isActive] bit NULL,
    CONSTRAINT [PK__CampType__3EADA5F5A74792CC] PRIMARY KEY ([campTypeId])
);
GO

CREATE TABLE [ChatRoom] (
    [chatRoomId] int NOT NULL IDENTITY,
    [name] nvarchar(255) NULL,
    CONSTRAINT [PK__ChatRoom__CB58B49253ED31B5] PRIMARY KEY ([chatRoomId])
);
GO

CREATE TABLE [FAQ] (
    [faqId] int NOT NULL IDENTITY,
    [question] nvarchar(255) NULL,
    [answer] nvarchar(max) NULL,
    CONSTRAINT [PK__FAQ__B18E4567C2723C94] PRIMARY KEY ([faqId])
);
GO

CREATE TABLE [Guardian] (
    [guardianId] int NOT NULL IDENTITY,
    [fullName] nvarchar(255) NULL,
    [title] nvarchar(50) NULL,
    [gender] nvarchar(50) NULL,
    [dob] date NULL,
    [answer] nvarchar(255) NULL,
    [category] nvarchar(50) NULL,
    [isActive] bit NULL,
    CONSTRAINT [PK__Guardian__8A1718E14FC024F6] PRIMARY KEY ([guardianId])
);
GO

CREATE TABLE [PromotionType] (
    [promotionTypeId] int NOT NULL IDENTITY,
    [name] nvarchar(255) NULL,
    [description] nvarchar(max) NULL,
    [createAt] datetime NULL,
    [updateAt] datetime NULL,
    [status] nvarchar(50) NULL,
    CONSTRAINT [PK__Promotio__847F158D388561AD] PRIMARY KEY ([promotionTypeId])
);
GO

CREATE TABLE [UserAccount] (
    [userId] int NOT NULL IDENTITY,
    [firstName] nvarchar(255) NULL,
    [lastName] nvarchar(255) NULL,
    [email] varchar(255) NULL,
    [phoneNumber] varchar(255) NULL,
    [password] nvarchar(255) NULL,
    [role] nvarchar(50) NULL,
    [isActive] bit NULL,
    [createAt] datetime NULL,
    [avatar] nvarchar(255) NULL,
    [dob] date NULL,
    CONSTRAINT [PK__UserAcco__CB9A1CFF87D326B1] PRIMARY KEY ([userId])
);
GO

CREATE TABLE [VehicleType] (
    [vehicleTypeId] int NOT NULL IDENTITY,
    [name] nvarchar(255) NULL,
    [description] nvarchar(max) NULL,
    [isActive] bit NULL,
    CONSTRAINT [PK__VehicleT__4709A1D4F0649896] PRIMARY KEY ([vehicleTypeId])
);
GO

CREATE TABLE [BankUser] (
    [bankUserId] int NOT NULL IDENTITY,
    [userId] int NULL,
    [bankCode] varchar(50) NULL,
    [bankName] nvarchar(255) NULL,
    [bankNumber] varchar(255) NULL,
    [isPrimary] bit NULL,
    [isActive] bit NULL,
    CONSTRAINT [PK__BankUser__CBF11725229B75CF] PRIMARY KEY ([bankUserId]),
    CONSTRAINT [FK__BankUser__userId__7D439ABD] FOREIGN KEY ([userId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [Blog] (
    [blogId] int NOT NULL IDENTITY,
    [title] nvarchar(255) NULL,
    [content] nvarchar(max) NULL,
    [authorId] int NULL,
    [isActive] bit NULL,
    [createAt] datetime NULL,
    CONSTRAINT [PK__Blog__FA0AA72D8027A916] PRIMARY KEY ([blogId]),
    CONSTRAINT [FK__Blog__authorId__0880433F] FOREIGN KEY ([authorId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [ChatMessageAI] (
    [chatMessageAiId] int NOT NULL IDENTITY,
    [senderId] int NULL,
    [message] nvarchar(max) NULL,
    [response] nvarchar(max) NULL,
    [messageCreateAt] datetime NULL,
    [responseAt] datetime NULL,
    CONSTRAINT [PK__ChatMess__DBC8291A920D34D4] PRIMARY KEY ([chatMessageAiId]),
    CONSTRAINT [FK__ChatMessa__sende__607251E5] FOREIGN KEY ([senderId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [ChatRoomUser] (
    [chatRoomUserId] int NOT NULL IDENTITY,
    [chatRoomId] int NULL,
    [userId] int NULL,
    CONSTRAINT [PK__ChatRoom__C9D3D5664196E8D8] PRIMARY KEY ([chatRoomUserId]),
    CONSTRAINT [FK__ChatRoomU__chatR__57DD0BE4] FOREIGN KEY ([chatRoomId]) REFERENCES [ChatRoom] ([chatRoomId]),
    CONSTRAINT [FK__ChatRoomU__userI__58D1301D] FOREIGN KEY ([userId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [Driver] (
    [driverId] int NOT NULL IDENTITY,
    [userId] int NULL,
    [driverName] nvarchar(255) NULL,
    [driverPhoneNumber] varchar(255) NULL,
    [driverAddress] nvarchar(255) NULL,
    [driverLicense] varchar(255) NULL,
    [dob] date NULL,
    CONSTRAINT [PK__Driver__F1532DF2E68365B1] PRIMARY KEY ([driverId]),
    CONSTRAINT [FK__Driver__userId__02084FDA] FOREIGN KEY ([userId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [Livestream] (
    [livestreamId] int NOT NULL IDENTITY,
    [roomId] varchar(255) NULL,
    [title] nvarchar(255) NULL,
    [hostId] int NULL,
    CONSTRAINT [PK__Livestre__650D2CD3F56FAE02] PRIMARY KEY ([livestreamId]),
    CONSTRAINT [FK__Livestrea__hostI__634EBE90] FOREIGN KEY ([hostId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [Message] (
    [messageId] int NOT NULL IDENTITY,
    [chatRoomId] int NULL,
    [senderId] int NULL,
    [content] nvarchar(max) NULL,
    [receiverId] int NULL,
    [createAt] datetime NULL,
    CONSTRAINT [PK__Message__4808B9934C3F9FF7] PRIMARY KEY ([messageId]),
    CONSTRAINT [FK__Message__chatRoo__5BAD9CC8] FOREIGN KEY ([chatRoomId]) REFERENCES [ChatRoom] ([chatRoomId]),
    CONSTRAINT [FK__Message__receive__5D95E53A] FOREIGN KEY ([receiverId]) REFERENCES [UserAccount] ([userId]),
    CONSTRAINT [FK__Message__senderI__5CA1C101] FOREIGN KEY ([senderId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [Notifications] (
    [notificationId] int NOT NULL IDENTITY,
    [title] nvarchar(255) NULL,
    [message] nvarchar(max) NULL,
    [createAt] datetime NULL,
    [userId] int NULL,
    CONSTRAINT [PK__Notifica__4BA5CEA9134CC0BA] PRIMARY KEY ([notificationId]),
    CONSTRAINT [FK__Notificat__userI__7B264821] FOREIGN KEY ([userId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [Promotion] (
    [promotionId] int NOT NULL IDENTITY,
    [name] nvarchar(255) NULL,
    [description] nvarchar(max) NULL,
    [startDate] date NULL,
    [endDate] date NULL,
    [status] nvarchar(50) NULL,
    [percent] decimal(5,2) NULL,
    [maxDiscountAmount] decimal(10,2) NULL,
    [createBy] int NULL,
    [createAt] datetime NULL,
    [promotionTypeId] int NULL,
    [code] varchar(50) NULL,
    CONSTRAINT [PK__Promotio__99EB696EC39A7ACC] PRIMARY KEY ([promotionId]),
    CONSTRAINT [FK__Promotion__creat__0F624AF8] FOREIGN KEY ([createBy]) REFERENCES [UserAccount] ([userId]),
    CONSTRAINT [FK__Promotion__promo__10566F31] FOREIGN KEY ([promotionTypeId]) REFERENCES [PromotionType] ([promotionTypeId])
);
GO

CREATE TABLE [RefreshToken] (
    [id] int NOT NULL IDENTITY,
    [userId] int NOT NULL,
    [token] nvarchar(255) NOT NULL,
    [createdAt] datetime NOT NULL,
    [expiresAt] datetime NOT NULL,
    [isRevoked] bit NOT NULL,
    CONSTRAINT [PK__RefreshT__3213E83F646E62AB] PRIMARY KEY ([id]),
    CONSTRAINT [FK_RefreshToken_User] FOREIGN KEY ([userId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [Vehicle] (
    [vehicleId] int NOT NULL IDENTITY,
    [vehicleType] int NULL,
    [vehicleName] nvarchar(255) NULL,
    [vehicleNumber] varchar(255) NULL,
    [capacity] int NULL,
    [status] nvarchar(50) NULL,
    CONSTRAINT [PK__Vehicle__5B9D25F2463A0876] PRIMARY KEY ([vehicleId]),
    CONSTRAINT [FK__Vehicle__vehicle__4A8310C6] FOREIGN KEY ([vehicleType]) REFERENCES [VehicleType] ([vehicleTypeId])
);
GO

CREATE TABLE [LivestreamUser] (
    [livestreamUserId] int NOT NULL IDENTITY,
    [livestreamId] int NULL,
    [userId] int NULL,
    CONSTRAINT [PK__Livestre__A1E8B1E8EC9C9A7E] PRIMARY KEY ([livestreamUserId]),
    CONSTRAINT [FK__Livestrea__lives__662B2B3B] FOREIGN KEY ([livestreamId]) REFERENCES [Livestream] ([livestreamId]),
    CONSTRAINT [FK__Livestrea__userI__671F4F74] FOREIGN KEY ([userId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [DriverVehicle] (
    [driverVehicleId] int NOT NULL IDENTITY,
    [driverId] int NULL,
    [vehicleId] int NULL,
    CONSTRAINT [PK__DriverVe__D1A5D3882F71F9B3] PRIMARY KEY ([driverVehicleId]),
    CONSTRAINT [FK__DriverVeh__drive__4D5F7D71] FOREIGN KEY ([driverId]) REFERENCES [Driver] ([driverId]),
    CONSTRAINT [FK__DriverVeh__vehic__4E53A1AA] FOREIGN KEY ([vehicleId]) REFERENCES [Vehicle] ([vehicleId])
);
GO

CREATE TABLE [Accommodation] (
    [accommodationId] int NOT NULL IDENTITY,
    [campId] int NULL,
    [accommodationTypeId] int NULL,
    [name] nvarchar(255) NULL,
    [capacity] int NULL,
    [isActive] bit NULL,
    CONSTRAINT [PK__Accommod__20C0A5FD5401C017] PRIMARY KEY ([accommodationId]),
    CONSTRAINT [FK__Accommoda__accom__1CBC4616] FOREIGN KEY ([accommodationTypeId]) REFERENCES [AccommodationType] ([accommodationTypeId])
);
GO

CREATE TABLE [Activity] (
    [activityId] int NOT NULL IDENTITY,
    [activityType] nvarchar(255) NULL,
    [name] nvarchar(255) NULL,
    [description] nvarchar(max) NULL,
    [campId] int NULL,
    [locationId] int NULL,
    CONSTRAINT [PK__Activity__0FC9CBEC92704BD9] PRIMARY KEY ([activityId])
);
GO

CREATE TABLE [Album] (
    [albumId] int NOT NULL IDENTITY,
    [campId] int NULL,
    [date] date NULL,
    [title] nvarchar(255) NULL,
    [description] nvarchar(max) NULL,
    CONSTRAINT [PK__Album__75BF3ECFA51B9673] PRIMARY KEY ([albumId])
);
GO

CREATE TABLE [AlbumPhoto] (
    [albumPhotoId] int NOT NULL IDENTITY,
    [albumId] int NULL,
    [photo] nvarchar(255) NULL,
    [caption] nvarchar(255) NULL,
    CONSTRAINT [PK__AlbumPho__695404581FC4A374] PRIMARY KEY ([albumPhotoId]),
    CONSTRAINT [FK__AlbumPhot__album__00DF2177] FOREIGN KEY ([albumId]) REFERENCES [Album] ([albumId])
);
GO

CREATE TABLE [AlbumPhotoFace] (
    [albumPhotoFaceId] int NOT NULL IDENTITY,
    [albumPhotoId] int NOT NULL,
    [faceTemplate] varbinary(max) NOT NULL,
    CONSTRAINT [PK__AlbumPho__CCCCD35F5649B2BA] PRIMARY KEY ([albumPhotoFaceId]),
    CONSTRAINT [FK_AlbumPhotoFace_AlbumPhoto] FOREIGN KEY ([albumPhotoId]) REFERENCES [AlbumPhoto] ([albumPhotoId])
);
GO

CREATE TABLE [AttendanceLog] (
    [attendanceLogId] int NOT NULL IDENTITY,
    [camperId] int NOT NULL,
    [timestamp] datetime NOT NULL,
    [eventType] nvarchar(50) NOT NULL,
    [checkInMethod] nvarchar(50) NOT NULL,
    [vehicleId] int NULL,
    [activityId] int NULL,
    [staffId] int NULL,
    [note] nvarchar(255) NULL,
    CONSTRAINT [PK__Attendan__F147FFAE82437F18] PRIMARY KEY ([attendanceLogId]),
    CONSTRAINT [FK_AttendanceLog_Activity] FOREIGN KEY ([activityId]) REFERENCES [Activity] ([activityId]),
    CONSTRAINT [FK_AttendanceLog_Staff] FOREIGN KEY ([staffId]) REFERENCES [UserAccount] ([userId]),
    CONSTRAINT [FK_AttendanceLog_Vehicle] FOREIGN KEY ([vehicleId]) REFERENCES [Vehicle] ([vehicleId])
);
GO

CREATE TABLE [Camp] (
    [campId] int NOT NULL IDENTITY,
    [name] nvarchar(255) NULL,
    [description] nvarchar(max) NULL,
    [place] nvarchar(255) NULL,
    [address] nvarchar(255) NULL,
    [minParticipants] int NULL,
    [maxParticipants] int NULL,
    [startDate] date NULL,
    [endDate] date NULL,
    [price] decimal(10,2) NULL,
    [status] nvarchar(50) NULL,
    [campTypeId] int NULL,
    [image] nvarchar(255) NULL,
    [createBy] int NULL,
    [locationId] int NULL,
    [promotionId] int NULL,
    [registrationStartDate] datetime NULL,
    [registrationEndDate] datetime NULL,
    [minAge] int NULL,
    [maxAge] int NULL,
    CONSTRAINT [PK__Camp__BC586B191C6014DA] PRIMARY KEY ([campId]),
    CONSTRAINT [FK_Camp_Promotion] FOREIGN KEY ([promotionId]) REFERENCES [Promotion] ([promotionId]),
    CONSTRAINT [FK__Camp__campTypeId__04E4BC85] FOREIGN KEY ([campTypeId]) REFERENCES [CampType] ([campTypeId]),
    CONSTRAINT [FK__Camp__createBy__05D8E0BE] FOREIGN KEY ([createBy]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [CampBadge] (
    [campBadgeId] int NOT NULL IDENTITY,
    [badgeId] int NULL,
    [campId] int NULL,
    CONSTRAINT [PK__CampBadg__7452AA76F8B74F8D] PRIMARY KEY ([campBadgeId]),
    CONSTRAINT [FK__CampBadge__badge__208CD6FA] FOREIGN KEY ([badgeId]) REFERENCES [Badge] ([badgeId]),
    CONSTRAINT [FK__CampBadge__campI__2180FB33] FOREIGN KEY ([campId]) REFERENCES [Camp] ([campId])
);
GO

CREATE TABLE [CamperGroup] (
    [camperGroupId] int NOT NULL IDENTITY,
    [groupName] nvarchar(255) NULL,
    [description] nvarchar(max) NULL,
    [maxSize] int NULL,
    [supervisorId] int NULL,
    [campId] int NULL,
    [minAge] int NULL,
    [maxAge] int NULL,
    CONSTRAINT [PK__CamperGr__A3F9F2EB18FD70C7] PRIMARY KEY ([camperGroupId]),
    CONSTRAINT [FK__CamperGro__campI__18EBB532] FOREIGN KEY ([campId]) REFERENCES [Camp] ([campId]),
    CONSTRAINT [FK__CamperGro__super__17F790F9] FOREIGN KEY ([supervisorId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [CampStaffAssignment] (
    [campStaffAssignmentId] int NOT NULL IDENTITY,
    [staffId] int NULL,
    [campId] int NULL,
    CONSTRAINT [PK__ManagerA__703B219833FB8987] PRIMARY KEY ([campStaffAssignmentId]),
    CONSTRAINT [FK__ManagerAs__campI__151B244E] FOREIGN KEY ([campId]) REFERENCES [Camp] ([campId]),
    CONSTRAINT [FK__ManagerAs__manag__14270015] FOREIGN KEY ([staffId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [DriverSchedule] (
    [driverScheduleId] int NOT NULL IDENTITY,
    [driverId] int NULL,
    [vehicleId] int NULL,
    [campId] int NULL,
    [workDate] date NULL,
    [startTime] time NULL,
    [endTime] time NULL,
    [status] nvarchar(50) NULL,
    CONSTRAINT [PK__DriverSc__8ACADD5F9F179836] PRIMARY KEY ([driverScheduleId]),
    CONSTRAINT [FK__DriverSch__campI__531856C7] FOREIGN KEY ([campId]) REFERENCES [Camp] ([campId]),
    CONSTRAINT [FK__DriverSch__drive__51300E55] FOREIGN KEY ([driverId]) REFERENCES [Driver] ([driverId]),
    CONSTRAINT [FK__DriverSch__vehic__5224328E] FOREIGN KEY ([vehicleId]) REFERENCES [Vehicle] ([vehicleId])
);
GO

CREATE TABLE [Registration] (
    [registrationId] int NOT NULL IDENTITY,
    [campId] int NULL,
    [registrationCreateAt] datetime NULL,
    [status] nvarchar(50) NULL,
    [appliedPromotionId] int NULL,
    [note] nvarchar(max) NULL,
    [userId] int NULL,
    CONSTRAINT [PK__Registra__A3DB1435EB987530] PRIMARY KEY ([registrationId]),
    CONSTRAINT [FK_Registration_AppliedPromotion] FOREIGN KEY ([appliedPromotionId]) REFERENCES [Promotion] ([promotionId]),
    CONSTRAINT [FK_Registration_UserAccount] FOREIGN KEY ([userId]) REFERENCES [UserAccount] ([userId]),
    CONSTRAINT [FK__Registrat__campI__6CD828CA] FOREIGN KEY ([campId]) REFERENCES [Camp] ([campId])
);
GO

CREATE TABLE [Route] (
    [routeId] int NOT NULL IDENTITY,
    [campId] int NULL,
    [routeName] nvarchar(255) NULL,
    [status] nvarchar(50) NULL,
    CONSTRAINT [PK__Route__BAC024C756723709] PRIMARY KEY ([routeId]),
    CONSTRAINT [FK__Route__campId__08B54D69] FOREIGN KEY ([campId]) REFERENCES [Camp] ([campId])
);
GO

CREATE TABLE [Camper] (
    [camperId] int NOT NULL IDENTITY,
    [camperName] nvarchar(255) NULL,
    [gender] nvarchar(50) NULL,
    [groupId] int NULL,
    [dob] date NULL,
    [faceTemplate] varbinary(max) NULL,
    [avatar] nvarchar(max) NULL,
    CONSTRAINT [PK__Camper__1F5EA63223697F05] PRIMARY KEY ([camperId]),
    CONSTRAINT [FK__Camper__groupId__245D67DE] FOREIGN KEY ([groupId]) REFERENCES [CamperGroup] ([camperGroupId])
);
GO

CREATE TABLE [GroupActivity] (
    [groupActivityId] int NOT NULL IDENTITY,
    [camperGroupId] int NULL,
    [activityId] int NULL,
    [status] nvarchar(50) NULL,
    CONSTRAINT [PK__GroupAct__4CEA4BBCEF38C6BA] PRIMARY KEY ([groupActivityId]),
    CONSTRAINT [FK__GroupActi__activ__3E1D39E1] FOREIGN KEY ([activityId]) REFERENCES [Activity] ([activityId]),
    CONSTRAINT [FK__GroupActi__campe__3D2915A8] FOREIGN KEY ([camperGroupId]) REFERENCES [CamperGroup] ([camperGroupId])
);
GO

CREATE TABLE [Feedback] (
    [feedbackId] int NOT NULL IDENTITY,
    [registrationId] int NULL,
    [userId] int NULL,
    [rating] int NULL,
    [comment] nvarchar(max) NULL,
    [createAt] datetime NULL,
    [campId] int NULL,
    CONSTRAINT [PK__Feedback__2613FD244309CC64] PRIMARY KEY ([feedbackId]),
    CONSTRAINT [FK__Feedback__campId__7849DB76] FOREIGN KEY ([campId]) REFERENCES [Camp] ([campId]),
    CONSTRAINT [FK__Feedback__regist__76619304] FOREIGN KEY ([registrationId]) REFERENCES [Registration] ([registrationId]),
    CONSTRAINT [FK__Feedback__userId__7755B73D] FOREIGN KEY ([userId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [RegistrationCancel] (
    [registrationCancelId] int NOT NULL IDENTITY,
    [registrationId] int NULL,
    [reason] nvarchar(max) NULL,
    [imageRefund] nvarchar(255) NULL,
    CONSTRAINT [PK__Registra__1BFD300A16D0BB46] PRIMARY KEY ([registrationCancelId]),
    CONSTRAINT [FK__Registrat__regis__73852659] FOREIGN KEY ([registrationId]) REFERENCES [Registration] ([registrationId])
);
GO

CREATE TABLE [Transaction] (
    [transactionId] int NOT NULL IDENTITY,
    [registrationId] int NULL,
    [amount] decimal(10,2) NULL,
    [status] nvarchar(50) NULL,
    [type] nvarchar(50) NULL,
    [transactionTime] datetime NULL,
    [method] nvarchar(50) NULL,
    [transactionCode] nvarchar(255) NULL,
    CONSTRAINT [PK__Transact__9B57CF7216BC3CDE] PRIMARY KEY ([transactionId]),
    CONSTRAINT [FK__Transacti__regis__70A8B9AE] FOREIGN KEY ([registrationId]) REFERENCES [Registration] ([registrationId])
);
GO

CREATE TABLE [Location] (
    [locationId] int NOT NULL IDENTITY,
    [routeId] int NULL,
    [name] nvarchar(255) NULL,
    [locationType] nvarchar(50) NULL,
    [isActive] bit NULL,
    CONSTRAINT [PK__Location__30646B6E1B427B16] PRIMARY KEY ([locationId]),
    CONSTRAINT [FK__Location__routeI__0B91BA14] FOREIGN KEY ([routeId]) REFERENCES [Route] ([routeId])
);
GO

CREATE TABLE [VehicleSchedule] (
    [vehicleScheduleId] int NOT NULL IDENTITY,
    [vehicleId] int NULL,
    [routeId] int NULL,
    [date] date NULL,
    [status] nvarchar(50) NULL,
    CONSTRAINT [PK__VehicleS__98B042A10CA86140] PRIMARY KEY ([vehicleScheduleId]),
    CONSTRAINT [FK__VehicleSc__route__0C50D423] FOREIGN KEY ([routeId]) REFERENCES [Route] ([routeId]),
    CONSTRAINT [FK__VehicleSc__vehic__0B5CAFEA] FOREIGN KEY ([vehicleId]) REFERENCES [Vehicle] ([vehicleId])
);
GO

CREATE TABLE [CamperAccommodation] (
    [camperAccommodationId] int NOT NULL IDENTITY,
    [camperId] int NOT NULL,
    [accommodationId] int NOT NULL,
    [assignedAt] datetime NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__CamperAc__3784380FF16C0712] PRIMARY KEY ([camperAccommodationId]),
    CONSTRAINT [FK_CamperAccommodation_Accommodation] FOREIGN KEY ([accommodationId]) REFERENCES [Accommodation] ([accommodationId]),
    CONSTRAINT [FK_CamperAccommodation_Camper] FOREIGN KEY ([camperId]) REFERENCES [Camper] ([camperId])
);
GO

CREATE TABLE [CamperActivity] (
    [camperActivityId] int NOT NULL IDENTITY,
    [camperId] int NULL,
    [activityId] int NULL,
    [participationStatus] nvarchar(50) NULL,
    CONSTRAINT [PK__CamperAc__B77C246F3CB74DB4] PRIMARY KEY ([camperActivityId]),
    CONSTRAINT [FK__CamperAct__activ__3A4CA8FD] FOREIGN KEY ([activityId]) REFERENCES [Activity] ([activityId]),
    CONSTRAINT [FK__CamperAct__campe__395884C4] FOREIGN KEY ([camperId]) REFERENCES [Camper] ([camperId])
);
GO

CREATE TABLE [CamperBadge] (
    [camperBadgeId] int NOT NULL IDENTITY,
    [camperId] int NULL,
    [badgeId] int NULL,
    CONSTRAINT [PK__CamperBa__67972E47D42C121E] PRIMARY KEY ([camperBadgeId]),
    CONSTRAINT [FK__CamperBad__badge__32AB8735] FOREIGN KEY ([badgeId]) REFERENCES [Badge] ([badgeId]),
    CONSTRAINT [FK__CamperBad__campe__31B762FC] FOREIGN KEY ([camperId]) REFERENCES [Camper] ([camperId])
);
GO

CREATE TABLE [CamperGuardian] (
    [camperGuardianId] int NOT NULL IDENTITY,
    [camperId] int NULL,
    [guardianId] int NULL,
    CONSTRAINT [PK__CamperGu__B304497C5424285B] PRIMARY KEY ([camperGuardianId]),
    CONSTRAINT [FK__CamperGua__campe__2739D489] FOREIGN KEY ([camperId]) REFERENCES [Camper] ([camperId]),
    CONSTRAINT [FK__CamperGua__guard__282DF8C2] FOREIGN KEY ([guardianId]) REFERENCES [Guardian] ([guardianId])
);
GO

CREATE TABLE [HealthRecord] (
    [healthRecordId] int NOT NULL IDENTITY,
    [condition] nvarchar(max) NULL,
    [allergies] nvarchar(max) NULL,
    [isAllergy] bit NULL,
    [note] nvarchar(max) NULL,
    [createAt] datetime NULL,
    [camperId] int NULL,
    CONSTRAINT [PK__HealthRe__59B2D406F4033F3E] PRIMARY KEY ([healthRecordId]),
    CONSTRAINT [FK__HealthRec__campe__2EDAF651] FOREIGN KEY ([camperId]) REFERENCES [Camper] ([camperId])
);
GO

CREATE TABLE [Incident] (
    [incidentId] int NOT NULL IDENTITY,
    [description] nvarchar(max) NULL,
    [incidentDate] date NULL,
    [status] nvarchar(50) NULL,
    [note] nvarchar(max) NULL,
    [camperId] int NULL,
    [campStaffId] int NULL,
    [campId] int NULL,
    CONSTRAINT [PK__Incident__06A5D741842EED80] PRIMARY KEY ([incidentId]),
    CONSTRAINT [FK__Incident__campId__47A6A41B] FOREIGN KEY ([campId]) REFERENCES [Camp] ([campId]),
    CONSTRAINT [FK__Incident__campSt__46B27FE2] FOREIGN KEY ([campStaffId]) REFERENCES [UserAccount] ([userId]),
    CONSTRAINT [FK__Incident__camper__45BE5BA9] FOREIGN KEY ([camperId]) REFERENCES [Camper] ([camperId])
);
GO

CREATE TABLE [ParentCamper] (
    [parentCamperId] int NOT NULL IDENTITY,
    [parentId] int NULL,
    [camperId] int NULL,
    [relationship] nvarchar(255) NULL,
    CONSTRAINT [PK__ParentCa__6645EA93BF9107B7] PRIMARY KEY ([parentCamperId]),
    CONSTRAINT [FK__ParentCam__campe__2BFE89A6] FOREIGN KEY ([camperId]) REFERENCES [Camper] ([camperId]),
    CONSTRAINT [FK__ParentCam__paren__2B0A656D] FOREIGN KEY ([parentId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [RegistrationCamper] (
    [registrationId] int NOT NULL,
    [camperId] int NOT NULL,
    CONSTRAINT [PK__Registra__922EFE5614C859DF] PRIMARY KEY ([registrationId], [camperId]),
    CONSTRAINT [FK__Registrat__campe__308E3499] FOREIGN KEY ([camperId]) REFERENCES [Camper] ([camperId]) ON DELETE CASCADE,
    CONSTRAINT [FK__Registrat__regis__2F9A1060] FOREIGN KEY ([registrationId]) REFERENCES [Registration] ([registrationId]) ON DELETE CASCADE
);
GO

CREATE TABLE [Report] (
    [reportId] int NOT NULL IDENTITY,
    [camperId] int NULL,
    [note] nvarchar(max) NULL,
    [image] nvarchar(255) NULL,
    [createAt] datetime NULL,
    [status] nvarchar(50) NULL,
    [reportedBy] int NULL,
    [activityId] int NULL,
    [level] nvarchar(50) NULL,
    CONSTRAINT [PK__Report__1C9B4E2D17DAFC9F] PRIMARY KEY ([reportId]),
    CONSTRAINT [FK__Report__activity__42E1EEFE] FOREIGN KEY ([activityId]) REFERENCES [Activity] ([activityId]),
    CONSTRAINT [FK__Report__camperId__40F9A68C] FOREIGN KEY ([camperId]) REFERENCES [Camper] ([camperId]),
    CONSTRAINT [FK__Report__reported__41EDCAC5] FOREIGN KEY ([reportedBy]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE TABLE [Visitation] (
    [visitationId] int NOT NULL IDENTITY,
    [userId] int NULL,
    [camperId] int NULL,
    [visitStartTime] datetime NULL,
    [visitEndTime] datetime NULL,
    [approvedBy] int NULL,
    [CheckInTime] datetime NULL,
    [CheckOutTime] datetime NULL,
    [createAt] datetime NULL,
    [updateAt] datetime NULL,
    [status] nvarchar(50) NULL,
    CONSTRAINT [PK__Visitati__E33BAE4142B08E97] PRIMARY KEY ([visitationId]),
    CONSTRAINT [FK__Visitatio__appro__05A3D694] FOREIGN KEY ([approvedBy]) REFERENCES [UserAccount] ([userId]),
    CONSTRAINT [FK__Visitatio__campe__04AFB25B] FOREIGN KEY ([camperId]) REFERENCES [Camper] ([camperId]),
    CONSTRAINT [FK__Visitatio__userI__03BB8E22] FOREIGN KEY ([userId]) REFERENCES [UserAccount] ([userId])
);
GO

CREATE INDEX [IX_Accommodation_accommodationTypeId] ON [Accommodation] ([accommodationTypeId]);
GO

CREATE INDEX [IX_Accommodation_campId] ON [Accommodation] ([campId]);
GO

CREATE INDEX [IX_Activity_campId] ON [Activity] ([campId]);
GO

CREATE INDEX [IX_Activity_locationId] ON [Activity] ([locationId]);
GO

CREATE INDEX [IX_Album_campId] ON [Album] ([campId]);
GO

CREATE INDEX [IX_AlbumPhoto_albumId] ON [AlbumPhoto] ([albumId]);
GO

CREATE INDEX [IX_AlbumPhotoFace_albumPhotoId] ON [AlbumPhotoFace] ([albumPhotoId]);
GO

CREATE INDEX [IX_AttendanceLog_activityId] ON [AttendanceLog] ([activityId]);
GO

CREATE INDEX [IX_AttendanceLog_camperId] ON [AttendanceLog] ([camperId]);
GO

CREATE INDEX [IX_AttendanceLog_staffId] ON [AttendanceLog] ([staffId]);
GO

CREATE INDEX [IX_AttendanceLog_vehicleId] ON [AttendanceLog] ([vehicleId]);
GO

CREATE INDEX [IX_BankUser_userId] ON [BankUser] ([userId]);
GO

CREATE INDEX [IX_Blog_authorId] ON [Blog] ([authorId]);
GO

CREATE INDEX [IX_Camp_campTypeId] ON [Camp] ([campTypeId]);
GO

CREATE INDEX [IX_Camp_createBy] ON [Camp] ([createBy]);
GO

CREATE INDEX [IX_Camp_locationId] ON [Camp] ([locationId]);
GO

CREATE INDEX [IX_Camp_promotionId] ON [Camp] ([promotionId]);
GO

CREATE INDEX [IX_CampBadge_badgeId] ON [CampBadge] ([badgeId]);
GO

CREATE INDEX [IX_CampBadge_campId] ON [CampBadge] ([campId]);
GO

CREATE INDEX [IX_Camper_groupId] ON [Camper] ([groupId]);
GO

CREATE INDEX [IX_CamperAccommodation_accommodationId] ON [CamperAccommodation] ([accommodationId]);
GO

CREATE INDEX [IX_CamperAccommodation_camperId] ON [CamperAccommodation] ([camperId]);
GO

CREATE INDEX [IX_CamperActivity_activityId] ON [CamperActivity] ([activityId]);
GO

CREATE INDEX [IX_CamperActivity_camperId] ON [CamperActivity] ([camperId]);
GO

CREATE INDEX [IX_CamperBadge_badgeId] ON [CamperBadge] ([badgeId]);
GO

CREATE INDEX [IX_CamperBadge_camperId] ON [CamperBadge] ([camperId]);
GO

CREATE INDEX [IX_CamperGroup_campId] ON [CamperGroup] ([campId]);
GO

CREATE INDEX [IX_CamperGroup_supervisorId] ON [CamperGroup] ([supervisorId]);
GO

CREATE INDEX [IX_CamperGuardian_camperId] ON [CamperGuardian] ([camperId]);
GO

CREATE INDEX [IX_CamperGuardian_guardianId] ON [CamperGuardian] ([guardianId]);
GO

CREATE INDEX [IX_CampStaffAssignment_campId] ON [CampStaffAssignment] ([campId]);
GO

CREATE INDEX [IX_CampStaffAssignment_staffId] ON [CampStaffAssignment] ([staffId]);
GO

CREATE INDEX [IX_ChatMessageAI_senderId] ON [ChatMessageAI] ([senderId]);
GO

CREATE INDEX [IX_ChatRoomUser_chatRoomId] ON [ChatRoomUser] ([chatRoomId]);
GO

CREATE INDEX [IX_ChatRoomUser_userId] ON [ChatRoomUser] ([userId]);
GO

CREATE INDEX [IX_Driver_userId] ON [Driver] ([userId]);
GO

CREATE INDEX [IX_DriverSchedule_campId] ON [DriverSchedule] ([campId]);
GO

CREATE INDEX [IX_DriverSchedule_driverId] ON [DriverSchedule] ([driverId]);
GO

CREATE INDEX [IX_DriverSchedule_vehicleId] ON [DriverSchedule] ([vehicleId]);
GO

CREATE INDEX [IX_DriverVehicle_driverId] ON [DriverVehicle] ([driverId]);
GO

CREATE INDEX [IX_DriverVehicle_vehicleId] ON [DriverVehicle] ([vehicleId]);
GO

CREATE INDEX [IX_Feedback_campId] ON [Feedback] ([campId]);
GO

CREATE INDEX [IX_Feedback_registrationId] ON [Feedback] ([registrationId]);
GO

CREATE INDEX [IX_Feedback_userId] ON [Feedback] ([userId]);
GO

CREATE INDEX [IX_GroupActivity_activityId] ON [GroupActivity] ([activityId]);
GO

CREATE INDEX [IX_GroupActivity_camperGroupId] ON [GroupActivity] ([camperGroupId]);
GO

CREATE UNIQUE INDEX [UQ_HealthRecord_CamperId] ON [HealthRecord] ([camperId]) WHERE [camperId] IS NOT NULL;
GO

CREATE INDEX [IX_Incident_camperId] ON [Incident] ([camperId]);
GO

CREATE INDEX [IX_Incident_campId] ON [Incident] ([campId]);
GO

CREATE INDEX [IX_Incident_campStaffId] ON [Incident] ([campStaffId]);
GO

CREATE INDEX [IX_Livestream_hostId] ON [Livestream] ([hostId]);
GO

CREATE INDEX [IX_LivestreamUser_livestreamId] ON [LivestreamUser] ([livestreamId]);
GO

CREATE INDEX [IX_LivestreamUser_userId] ON [LivestreamUser] ([userId]);
GO

CREATE INDEX [IX_Location_routeId] ON [Location] ([routeId]);
GO

CREATE INDEX [IX_Message_chatRoomId] ON [Message] ([chatRoomId]);
GO

CREATE INDEX [IX_Message_receiverId] ON [Message] ([receiverId]);
GO

CREATE INDEX [IX_Message_senderId] ON [Message] ([senderId]);
GO

CREATE INDEX [IX_Notifications_userId] ON [Notifications] ([userId]);
GO

CREATE INDEX [IX_ParentCamper_camperId] ON [ParentCamper] ([camperId]);
GO

CREATE INDEX [IX_ParentCamper_parentId] ON [ParentCamper] ([parentId]);
GO

CREATE INDEX [IX_Promotion_createBy] ON [Promotion] ([createBy]);
GO

CREATE INDEX [IX_Promotion_promotionTypeId] ON [Promotion] ([promotionTypeId]);
GO

CREATE INDEX [IX_RefreshToken_userId] ON [RefreshToken] ([userId]);
GO

CREATE INDEX [IX_Registration_appliedPromotionId] ON [Registration] ([appliedPromotionId]);
GO

CREATE INDEX [IX_Registration_campId] ON [Registration] ([campId]);
GO

CREATE INDEX [IX_Registration_userId] ON [Registration] ([userId]);
GO

CREATE INDEX [IX_RegistrationCamper_camperId] ON [RegistrationCamper] ([camperId]);
GO

CREATE INDEX [IX_RegistrationCancel_registrationId] ON [RegistrationCancel] ([registrationId]);
GO

CREATE INDEX [IX_Report_activityId] ON [Report] ([activityId]);
GO

CREATE INDEX [IX_Report_camperId] ON [Report] ([camperId]);
GO

CREATE INDEX [IX_Report_reportedBy] ON [Report] ([reportedBy]);
GO

CREATE INDEX [IX_Route_campId] ON [Route] ([campId]);
GO

CREATE INDEX [IX_Transaction_registrationId] ON [Transaction] ([registrationId]);
GO

CREATE INDEX [IX_Vehicle_vehicleType] ON [Vehicle] ([vehicleType]);
GO

CREATE INDEX [IX_VehicleSchedule_routeId] ON [VehicleSchedule] ([routeId]);
GO

CREATE INDEX [IX_VehicleSchedule_vehicleId] ON [VehicleSchedule] ([vehicleId]);
GO

CREATE INDEX [IX_Visitation_approvedBy] ON [Visitation] ([approvedBy]);
GO

CREATE INDEX [IX_Visitation_camperId] ON [Visitation] ([camperId]);
GO

CREATE INDEX [IX_Visitation_userId] ON [Visitation] ([userId]);
GO

ALTER TABLE [Accommodation] ADD CONSTRAINT [FK__Accommoda__campI__1BC821DD] FOREIGN KEY ([campId]) REFERENCES [Camp] ([campId]);
GO

ALTER TABLE [Activity] ADD CONSTRAINT [FK_Activity_Camp] FOREIGN KEY ([campId]) REFERENCES [Camp] ([campId]);
GO

ALTER TABLE [Activity] ADD CONSTRAINT [FK_Activity_Location] FOREIGN KEY ([locationId]) REFERENCES [Location] ([locationId]);
GO

ALTER TABLE [Album] ADD CONSTRAINT [FK__Album__campId__7E02B4CC] FOREIGN KEY ([campId]) REFERENCES [Camp] ([campId]);
GO

ALTER TABLE [AttendanceLog] ADD CONSTRAINT [FK_AttendanceLog_Camper] FOREIGN KEY ([camperId]) REFERENCES [Camper] ([camperId]);
GO

ALTER TABLE [Camp] ADD CONSTRAINT [FK_Camp_Location] FOREIGN KEY ([locationId]) REFERENCES [Location] ([locationId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251021064036_InitialCreate', N'8.0.5');
GO

COMMIT;
GO

