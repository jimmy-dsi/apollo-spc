pub fn RingBuffer(comptime T: type, comptime N: usize) type {
    return struct {
        const Self = @This();

        pub const Iter = struct {
            parent:  *Self  = undefined,
            reverse:  bool  = false,
            current:  usize = 0,

            _cached: T = undefined,
            _cached_idx: usize = 0,
            _consumed: usize = 0,

            pub inline fn new(parent: *Self, reverse: bool) Iter {
                return Iter {
                    .parent  = parent,
                    .reverse = reverse,
                    .current = if (reverse) (parent._offset + parent.len - 1) % N else parent._offset
                };
            }

            pub fn step(self: *Iter) bool {
                if (self._consumed == self.parent.len) {
                    return false;
                }

                self._cached = self.parent._buf[self.current];
                self._cached_idx = self.current;

                self._consumed += 1;

                if (self.reverse) {
                    self.current = (self.current + N - 1) % N;
                }
                else {
                    self.current = (self.current + 1) % N;
                }

                return true;
            }

            pub inline fn done(self: *const Iter) bool {
                return self._consumed == self.parent.len;
            }

            pub fn get_reversed(self: *const Iter) Iter {
                var it = Iter.new(self.parent, !self.reverse);

                if (it.reverse) {
                    it.current = (self.current + N - 1) % N;
                }
                else {
                    it.current = (self.current + 1) % N;
                }

                it._consumed = self.parent.len - self._consumed;
                return it;
            }

            pub fn value(self: *const Iter) T {
                return self._cached;
            }

            pub fn index(self: *const Iter) usize {
                return self._cached_idx;
            }

            pub fn ref(self: *Iter) *T {
                return &self.parent._buf[self._cached_idx];
            }
        };

        len: usize = 0,

        _buf: [N]T = undefined,
        _offset: usize = 0,

        pub fn push(self: *Self, value: T) void {
            if (self.len == N) {
                const idx = (self.len + self._offset) % N;
                self._offset = (self._offset + 1) % N;
                self._buf[idx] = value;
            }
            else {
                self._buf[self.len] = value;
                self.len += 1;
            }
        }

        pub fn top(self: *const Self) T {
            const idx = (self.len + self._offset) % N;
            return self._buf[idx];
        }

        pub fn bottom(self: *const Self) T {
            const idx = self._offset;
            return self._buf[idx];
        }

        pub fn iter(self: *Self, reverse: bool) Iter {
            return Iter.new(self, reverse);
        }
    };
}