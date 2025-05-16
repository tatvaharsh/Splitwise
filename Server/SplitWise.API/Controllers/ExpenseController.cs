using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SplitWise.Service.Interface;

namespace SplitWise.API.Controllers;

[ApiController]
[Route("api/Group")]
public class ExpenseController(IActivityService activityService, IExpenseService expenseService, IAppContextService appContextService) : BaseController
{
    private readonly IActivityService _activityService = activityService;
    private readonly IExpenseService _expenseService = expenseService;
    private readonly IAppContextService _appContextService = appContextService;

    
}