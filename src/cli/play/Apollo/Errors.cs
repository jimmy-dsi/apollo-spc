using System.Diagnostics;

namespace Apollo;

public enum ResultType {
	Success              = 0,

	UnknownError         = 1,
	                        
	MultipleDeinit       = 2,
	AlreadyInited        = 3,
	InvalidState         = 4,
	NullPtr              = 5,
	                        
	MultipleMainEmu      = 6,
	                        
	AllocError           = 7,
	                        
	SpcMissingFileHeader = 8,
	SpcSizeTooShort      = 9,
	                        
	Script700Timeout     = 10,
	Script700LoadError   = 11,

	EmuIsNotMain         = 12,
}

public class Error: Exception {
	public static void Throw(uint errorCode) {
		if (errorCode == 0) {
			return;
		}
		
		switch ((ResultType) errorCode) {
			case ResultType.Success:              throw new UnreachableException();
			case ResultType.UnknownError:         throw new UnknownError();
			case ResultType.MultipleDeinit:       throw new DeinitError();
			case ResultType.AlreadyInited:        throw new InitError();
			case ResultType.InvalidState:         throw new StateError();
			case ResultType.NullPtr:              throw new NullError();
			case ResultType.MultipleMainEmu:      throw new MultipleMainEmuError();
			case ResultType.AllocError:           throw new AllocError();
			case ResultType.SpcMissingFileHeader: throw new SpcMissingHeaderError();
			case ResultType.SpcSizeTooShort:      throw new SpcSizeTooShortError();
			case ResultType.Script700Timeout:     throw new Script700Timeout();
			case ResultType.Script700LoadError:   throw new Script700LoadError();
			case ResultType.EmuIsNotMain:         throw new EmuNotMainError();
		}
	}
}

public class UnknownError:          Error { }
public class DeinitError:           Error { }
public class InitError:             Error { }
public class StateError:            Error { }
public class NullError:             Error { }
public class MultipleMainEmuError:  Error { }
public class AllocError:            Error { }
public class SpcMissingHeaderError: Error { }
public class SpcSizeTooShortError:  Error { }
public class Script700Timeout:      Error { }
public class Script700LoadError:    Error { }
public class EmuNotMainError:       Error { }