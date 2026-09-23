using System.ComponentModel.DataAnnotations;
using Trackr.Api.Models;

namespace Trackr.Api.Dtos;

public class UpdateIssueStatusRequest
{
    [EnumDataType(typeof(IssueStatus))]
    public required IssueStatus Status { get; set; }
}