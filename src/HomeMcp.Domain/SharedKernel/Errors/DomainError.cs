namespace HomeMcp.Domain.SharedKernel.Errors;

public abstract record DomainError(string Code, string Message);

public interface INotFoundError { }
public interface IValidationError { }
public interface IConflictError { }
public interface IUnauthorizedError { }
public interface IBusinessRuleError { }
