using System;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using Moq;
using FluentValidation;
using FluentValidation.Results;
using MindWeaveServer.BusinessLogic;
using MindWeaveServer.BusinessLogic.Abstractions;
using MindWeaveServer.DataAccess;
using MindWeaveServer.DataAccess.Abstractions;
using MindWeaveServer.Contracts.DataControls.Authentication;
using MindWeaveServer.Contracts.DataControls.Shared;
using MindWeaveServer.Utilities.Abstractions;
using MindWeaveServer.Utilities.Email;
using MindWeaveServer.Utilities.Email.Templates;

namespace MindWeaveServer.Tests.BusinessLogic
{
    public class AuthenticationLogicTests
    {
        private readonly Mock<IPlayerRepository> playerRepositoryMock;
        private readonly Mock<IEmailService> emailServiceMock;
        private readonly Mock<IPasswordService> passwordServiceMock;
        private readonly Mock<IPasswordPolicyValidator> passwordPolicyValidatorMock;
        private readonly Mock<IVerificationCodeService> verificationCodeServiceMock;
        private readonly Mock<IUserSessionManager> userSessionManagerMock;
        private readonly Mock<IValidator<UserProfileDto>> profileValidatorMock;
        private readonly Mock<IValidator<LoginDto>> loginValidatorMock;

        private readonly AuthenticationLogic authenticationLogic;

        public AuthenticationLogicTests()
        {
            playerRepositoryMock = new Mock<IPlayerRepository>();
            emailServiceMock = new Mock<IEmailService>();
            passwordServiceMock = new Mock<IPasswordService>();
            passwordPolicyValidatorMock = new Mock<IPasswordPolicyValidator>();
            verificationCodeServiceMock = new Mock<IVerificationCodeService>();
            userSessionManagerMock = new Mock<IUserSessionManager>();
            profileValidatorMock = new Mock<IValidator<UserProfileDto>>();
            loginValidatorMock = new Mock<IValidator<LoginDto>>();

            profileValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<UserProfileDto>(), default(CancellationToken)))
                .ReturnsAsync(new ValidationResult());

            loginValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<LoginDto>(), default(CancellationToken)))
                .ReturnsAsync(new ValidationResult());

            passwordPolicyValidatorMock.Setup(v => v.validate(It.IsAny<string>()))
                .Returns(new OperationResultDto { Success = true });

            authenticationLogic = new AuthenticationLogic(
                playerRepositoryMock.Object,
                emailServiceMock.Object,
                passwordServiceMock.Object,
                passwordPolicyValidatorMock.Object,
                verificationCodeServiceMock.Object,
                userSessionManagerMock.Object,
                profileValidatorMock.Object,
                loginValidatorMock.Object
            );
        }

        [Fact]
        public async Task RegisterPlayerAsync_NullProfile_ReturnsError()
        {
            var result = await authenticationLogic.registerPlayerAsync(null, "pass");
            Assert.False(result.Success);
        }

        [Fact]
        public async Task RegisterPlayerAsync_ValidationFail_ReturnsError()
        {
            var failure = new ValidationResult(new[] { new ValidationFailure("Prop", "Error") });
            profileValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<UserProfileDto>(), default(CancellationToken))).ReturnsAsync(failure);

            var result = await authenticationLogic.registerPlayerAsync(new UserProfileDto(), "pass");
            Assert.False(result.Success);
        }

        [Fact]
        public async Task RegisterPlayerAsync_WeakPassword_ReturnsError()
        {
            passwordPolicyValidatorMock.Setup(p => p.validate("weak")).Returns(new OperationResultDto { Success = false });

            var result = await authenticationLogic.registerPlayerAsync(new UserProfileDto(), "weak");
            Assert.False(result.Success);
        }

        [Fact]
        public async Task RegisterPlayerAsync_NewUser_CallsAddPlayer()
        {
            var dto = new UserProfileDto { Username = "New", Email = "e@e.com", FirstName = "F" };
            playerRepositoryMock.Setup(r => r.getPlayerByUsernameAsync("New")).ReturnsAsync((Player)null!);
            playerRepositoryMock.Setup(r => r.getPlayerByEmailAsync("e@e.com")).ReturnsAsync((Player)null!);

            await authenticationLogic.registerPlayerAsync(dto, "Pass123!");

            playerRepositoryMock.Verify(r => r.addPlayer(It.IsAny<Player>()), Times.Once);
        }

        [Fact]
        public async Task RegisterPlayerAsync_NewUser_SendsEmail()
        {
            var dto = new UserProfileDto { Username = "New", Email = "e@e.com", FirstName = "F" };
            await authenticationLogic.registerPlayerAsync(dto, "Pass");
            emailServiceMock.Verify(e => e.sendEmailAsync("e@e.com", "New", It.IsAny<IEmailTemplate>()), Times.Once);
        }

        [Fact]
        public async Task RegisterPlayerAsync_ExistingVerifiedUser_ReturnsFailure()
        {
            var existing = new Player { is_verified = true };
            playerRepositoryMock.Setup(r => r.getPlayerByUsernameAsync("Exist"))
                .ReturnsAsync(existing);

            var result = await authenticationLogic.registerPlayerAsync(
                new UserProfileDto { Username = "Exist" }, "Pass");

            Assert.Equal(MessageCodes.AUTH_USER_ALREADY_EXISTS, result.MessageCode);
        }

        [Fact]
        public async Task RegisterPlayerAsync_ExistingUnverifiedUser_UpdatesPlayer()
        {
            var existing = new Player { is_verified = false, username = "U", email = "e@e.com" };
            playerRepositoryMock.Setup(r => r.getPlayerByUsernameAsync("U")).ReturnsAsync(existing);

            var dto = new UserProfileDto { Username = "U", Email = "e@e.com", FirstName = "NewName" };

            await authenticationLogic.registerPlayerAsync(dto, "Pass");

            playerRepositoryMock.Verify(r => r.updatePlayerAsync(existing), Times.Once);
        }
    }
}