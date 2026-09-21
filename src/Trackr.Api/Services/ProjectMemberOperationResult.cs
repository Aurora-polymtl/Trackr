namespace Trackr.Api.Services;

public enum ProjectMemberOperationResult
{
    Success,
    ProjectNotFound,
    UserNotFound,
    OwnerCannotBeMember,
    MemberAlreadyExists
}