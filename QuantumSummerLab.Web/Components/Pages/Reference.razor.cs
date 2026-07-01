namespace QuantumSummerLab.Web.Components.Pages;

public partial class Reference
{
    private string _operation1 =
@"```js
operation Solve(q : Qubit) : Unit
{
    // Do something with qubits here
}
```";

    private string _operation2 =
@"```js
operation Main() : Unit
{
    // Your quantum experience starts here
}
```";

    private string _operation3 =
@"```js
operation Solve(q : Qubit) : Int
{
    // Do something with qubits here
    return 1;
}
```";

    private string _qubit1 =
@"```js
    use q = Qubit();
    use register = Qubit[3];
```";

    private string _qubit2 =
@"```js
    use q = Qubit();
    use register = Qubit[3];

    // Do something useful.

    Reset(q);
    ResetAll(register);
```";

    private string _call1 =
@"```js
    use q = Qubit();
    H(q);
    Reset(q);
```";

    private string _call2 =
@"```js
    use qs = Qubit[2];
    H(qs[0]);
    CNOT(qs[0], qs[1]);
    ResetAll(qs);
```";

    private string _variables1 =
@"```js
    use q = Qubit();
    H(q);
    let b = M(q);
    Reset(q);
```";

    private string _variables2 =
@"```js
    mutable countOfOnes = 0;

    for i in 1..100
    {
        use q = Qubit();
        H(q);
        if( M(q) == One )
        {
            set countOfOnes += 1;
        }
    }

    Reset(q);
```";

    private string _variables3 =
@"```js
    mutable countOfOnes = 0;

    for i in 1..100
    {
        use q = Qubit();
        H(q);
        set countOfOnes += M(q) == One ? 1 | 0;
    }

    Reset(q);
```";

    private string _debugging1 =
@"```js
    use q = Qubit();
    H(q);
    Message($""The state of the qubit after measurement is {M(q)}"");
    Reset(q);
```";

    private string _debugging2 =
@"```js
    use qs = Qubit[2];
    H(qs[0]);
    CNOT(qs[0], qs[1]);
    DumpRegister(qs);
    ResetAll(qs);
```";

    private string _debugging3 =
@"```txt
    Basis | Amplitude      | Probability | Phase
    --------------------------------------------
    |00⟩  | 0.7071+0.0000i | 50.0000%    | 0.0000
    |11⟩  | 0.7071+0.0000i | 50.0000%    | 0.0000
```";

    private async Task OnNavigateToHeading(string selector)
    {
        NavigationManager.NavigateTo($"/reference{selector}");
    }
}
