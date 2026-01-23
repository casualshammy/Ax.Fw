using Ax.Fw.Web.Interfaces;

namespace Ax.Fw.Web.Modules.RequestId;

internal record RequestIdImpl(Guid Id) : IRequestId;
