// RUN: %parallel-boogie "%s" > "%t"
// RUN: %diff "%s.expect" "%t"

// Insertion into a sorted array

type {:linear "tid"} Tid;
const nil:Tid;

var {:layer 0,2} A:[int]int;
var {:layer 0,2} count:int;
var {:layer 0,2} lock:Tid;

function {:inline} sorted (A:[int]int, count:int) : bool
{ (forall i:int, j:int :: 0 <= i && i <= j && j < count ==> A[i] <= A[j]) }

atomic action {:layer 2} INSERT ({:linear "tid"} tid:Tid, v:int)
modifies A, count;
{
  var idx:int; // index at which v is written

  assert count >= 0;
  assert sorted(A, count);

  assume 0 <= idx && idx <= count;
  assume (forall i:int :: 0 <= i && i < idx ==> A[i] < v);
  assume (forall i:int :: idx <= i && i < count ==> A[i] >= v);

  havoc A;

  assume (forall i:int :: i < idx ==> A[i] == old(A)[i]);
  assume (forall i:int :: idx < i && i < count ==> A[i+1] == old(A)[i]);
  assume (forall i:int :: count < i ==> A[i] == old(A)[i]);
  assume A[idx] == v;

  count := count + 1;
}

yield procedure {:layer 1} insert ({:linear "tid"} tid:Tid, v:int)
refines INSERT;
requires {:layer 1} tid != nil;
{
  var idx:int; // index at which v is written
  var j:int;   // loop counter used for shifting
  var a:int;   // value read from A
  var c:int;   // value read from count
  var {:layer 1} _A:[int]int;

  call {:layer 1} _A := Copy(A);
  call acquire(tid);

  idx := 0;
  call c := read_count(tid);
  call a := read_A(tid, idx);
  while (idx < c && a < v)
    invariant {:layer 1} 0 <= idx && idx <= count;
    invariant {:layer 1} a == A[idx];
    invariant {:layer 1} (forall i:int :: 0 <= i && i < idx ==> A[i] < v);
  {
    idx := idx + 1;
    call a := read_A(tid, idx);
  }
  j := c;
  while (idx < j)
    invariant {:layer 1} idx <= j && j <= count;
    invariant {:layer 1} (forall i:int :: i <= j ==> A[i] == _A[i]);
    invariant {:layer 1} (forall i:int :: j < i && i <= count ==> A[i] == _A[i-1]);
    invariant {:layer 1} (forall i:int :: count < i ==> A[i] == _A[i]);
  {
    call a := read_A(tid, j-1);
    call write_A(tid, j, a);
    j := j - 1;
  }
  call write_A(tid, idx, v);
  call write_count(tid, c+1);

  // let's see if we can prove that A is still sorted
  assert {:layer 1} sorted(A, count);

  call release(tid);
}

// =============================================================================

both action {:layer 1} READ_A ({:linear "tid"} tid:Tid, i:int) returns (v:int)
{
  assert tid != nil && lock == tid;
  v := A[i];
}

both action {:layer 1} WRITE_A ({:linear "tid"} tid:Tid, i:int, v:int)
modifies A;
{
  assert tid != nil && lock == tid;
  A[i] := v;
}

both action {:layer 1} READ_COUNT ({:linear "tid"} tid:Tid) returns (c:int)
{
  assert tid != nil && lock == tid;
  c := count;
}

both action {:layer 1} WRITE_COUNT ({:linear "tid"} tid:Tid, c:int)
modifies count;
{
  assert tid != nil && lock == tid;
  count := c;
}

right action {:layer 1} ACQUIRE ({:linear "tid"} tid:Tid)
modifies lock;
{
  assert tid != nil;
  assume lock == nil;
  lock := tid;
}

left action {:layer 1} RELEASE ({:linear "tid"} tid:Tid)
modifies lock;
{
  assert tid != nil && lock == tid;
  lock := nil;
}

yield procedure {:layer 0} read_A ({:linear "tid"} tid:Tid, i:int) returns (v:int);
refines READ_A;

yield procedure {:layer 0} write_A ({:linear "tid"} tid:Tid, i:int, v:int);
refines WRITE_A;

yield procedure {:layer 0} read_count ({:linear "tid"} tid:Tid) returns (c:int);
refines READ_COUNT;

yield procedure {:layer 0} write_count ({:linear "tid"} tid:Tid, c:int);
refines WRITE_COUNT;

yield procedure {:layer 0} acquire ({:linear "tid"} tid:Tid);
refines ACQUIRE;

yield procedure {:layer 0} release ({:linear "tid"} tid:Tid);
refines RELEASE;
