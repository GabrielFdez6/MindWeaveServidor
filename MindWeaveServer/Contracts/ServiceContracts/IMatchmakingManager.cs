using MindWeaveServer.Contracts.DataContracts.Game;
using MindWeaveServer.Contracts.DataContracts.Matchmaking;
using MindWeaveServer.Contracts.DataContracts.Puzzle;
using MindWeaveServer.Contracts.DataContracts.Shared;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;

namespace MindWeaveServer.Contracts.ServiceContracts
{
    [ServiceContract(CallbackContract = typeof(IMatchmakingCallback))]

    public interface IMatchmakingManager
    {
       

        [OperationContract]
        [FaultContract(typeof(ServiceFaultDto))]
        Task<GuestJoinResultDto> joinLobbyAsGuest(GuestJoinRequestDto joinRequest);


    }

    

    
}