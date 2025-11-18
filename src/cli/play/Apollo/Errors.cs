namespace Apollo;

public class Error: Exception { }

public class InitError:        Error { }
public class DeinitError:      Error { }
public class StateError:       Error { }
public class AllocError:       Error { }
public class NullError:        Error { }
public class Script700Timeout: Error { }