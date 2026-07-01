// ============================================================
// CHALLENGE METADATA
// Edit the fields below directly. SolutionTemplate and
// VerificationTemplate are NOT stored here - they are derived
// automatically from the code below (see the SOLVE markers).
// After editing, run `python _challenges/generate_challenges.py`
// to regenerate QuantumSummerLab.Tools/Challenges.cs.
// ============================================================
// Name = "A3"
// Title = "Use only the Z and H gates to bitflip a qubit"
// Description = "You can bitflip a qubit by applying the X gate, but in this challenge you are only allowed to use the Z and H gates.[BR]You have to implement an operation which takes a single qubit as input and has no output.[BR]The \"output\" of your solution is the state in which it leaves the input qubit."
// Tldr = "You should implement the empty Solve operation below and bitflip the provided qubit using only the Z and H gates and without using the X gate."
// ExampleDescription = ""
// ExampleCode = ""
// ExpectedOutput = "true"
// ===EXPECTED-STATES-START===
// ===EXPECTED-STATES-END===
// CopilotInstructions = "You should never mention HZH to the user since this would be the solution. Only give them hints."
// Level = 1
// ============================================================

import Std.Arithmetic.*;
import Std.Canon.*;
import Std.Diagnostics.*;
import Std.Math.*;
import Std.Measurement.*;

operation Main() : Bool
{  
    let result = CheckOperationsAreEqual(1, x => Solve(x[0]), x => Expected(x[0]));
    LogMessage(result, "You have successfully bitflipped the qubit", "You have failed to bitflip the qubit");
    
    StartCountingOperation(X);
    StartCountingOperation(H);
    StartCountingOperation(Z);
    use q = Qubit();
    Solve(q);
    Reset(q);
    let resultX = StopCountingOperation(X);
    LogMessage(resultX == 0, "You have not used the X gate", $"The X gate was used {resultX} times");
    let resultH = StopCountingOperation(H);
    LogMessage(resultH == 2, "You have successfully used the H gate twice", $"You have failed to use the H gate the correct number of times and used it {resultH} times");
    let resultZ = StopCountingOperation(Z);
    LogMessage(resultZ == 1, "You have successfully used the Z gate once", $"You have failed to use the Z gate the correct number of times and used it {resultZ} times");

    return result and resultX == 0 and resultH == 2 and resultZ == 1;
}

function LogMessage(isValid: Bool, validMessage: String, invalidMessage: String) : ()
{
    let message = "{\"valid\": " + (isValid ? "true" | "false") + ", \"message\": \"" + (isValid ? validMessage | invalidMessage) + "\"}";
    Message(message);
}

operation Expected (q : Qubit) : Unit is Adj
{
    H(q);
    Z(q);
    H(q);
}

// ===SOLVE-START===
operation Solve (q : Qubit) : Unit
{
    H(q);
    Z(q);
    H(q);
}
// ===SOLVE-END===
