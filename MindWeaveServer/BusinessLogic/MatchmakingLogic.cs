using MindWeaveServer.BusinessLogic.Abstractions;
using MindWeaveServer.BusinessLogic.Manager;
using MindWeaveServer.BusinessLogic.Models;
using MindWeaveServer.Contracts.DataContracts.Matchmaking;
using MindWeaveServer.Contracts.ServiceContracts;
using MindWeaveServer.DataAccess.Abstractions;
using MindWeaveServer.Contracts.DataContracts.Shared;
using NLog;
using System;
using System.Threading.Tasks;

namespace MindWeaveServer.BusinessLogic
{
    public class MatchmakingLogic
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly ILobbyLifecycleService lifecycleService;
        private readonly ILobbyInteractionService interactionService;
        private readonly INotificationService notificationService;
        private readonly IGameStateManager gameStateManager;
        private readonly GameSessionManager gameSessionManager;
        private readonly IPlayerRepository playerRepository;
        private readonly IMatchmakingRepository matchmakingRepository;
        private readonly LobbyModerationManager moderationManager;

        private const int ID_REASON_PROFANITY = 2;
        public const int INVALID_PLAYER_ID = 0;
        public const string PROFANITY_REASON_TEXT = "Profanity";



        public async Task<GuestJoinResultDto> joinLobbyAsGuestAsync(GuestJoinRequestDto joinRequest, IMatchmakingCallback callback)
        {
            return await lifecycleService.joinLobbyAsGuestAsync(joinRequest, callback);
        }


        public async Task inviteGuestByEmailAsync(GuestInvitationDto invitationData)
        {
            if (invitationData == null)
            {
                return;
            }
            await interactionService.inviteGuestByEmailAsync(invitationData.InviterUsername, invitationData.LobbyCode, invitationData.GuestEmail);
        }


    }
}