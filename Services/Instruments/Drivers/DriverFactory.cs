using ZDLISPlus.Web.Models;

namespace ZDLISPlus.Web.Services.Instruments.Drivers;

using ZDLISPlus.Web.Services.Instruments.Parsing;

public class DriverFactory
{
    private readonly IServiceProvider _sp;
    public DriverFactory(IServiceProvider sp) => _sp = sp;

    public IInstrumentDriver Create(Instrument instrument)
    {
        return instrument.InterfaceKind switch
        {
            InterfaceKind.TcpClient => new TcpClientDriver(instrument, _sp),
            InterfaceKind.TcpServer => new TcpServerDriver(instrument, _sp),
            InterfaceKind.Serial => new SerialDriver(instrument, _sp),
            InterfaceKind.File => new FileDriver(instrument, _sp),
            _ => new TcpServerDriver(instrument, _sp)
        };
    }
}