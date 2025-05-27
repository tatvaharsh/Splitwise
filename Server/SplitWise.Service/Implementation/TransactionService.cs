using SplitWise.Domain.Data;
using SplitWise.Repository.Interface;
using SplitWise.Service.Interface;

namespace SplitWise.Service.Implementation;

public class TransactionService(IBaseRepository<Transaction> baseRepository) : BaseService<Transaction>(baseRepository), ITransactionService
{
}
