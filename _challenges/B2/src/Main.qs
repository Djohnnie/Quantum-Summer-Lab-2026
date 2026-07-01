// ============================================================
// CHALLENGE METADATA
// Edit the fields below directly. SolutionTemplate and
// VerificationTemplate are NOT stored here - they are derived
// automatically from the code below (see the SOLVE markers).
// After editing, run `python _challenges/generate_challenges.py`
// to regenerate QuantumSummerLab.Tools/Challenges.cs.
// ============================================================
// Name = "B2"
// Title = "Distinguish H from X"
// Description = "You are given an operation that implements a single-qubit unitary transformation: either the Hadamard gate (H gate) or the bit-flip gate (X gate).[BR]Your task is to perform necessary operations and measurements to figure out which unitary it was and to return 0 if it was the H gate or 1 if it was the X gate.[BR]You are allowed to apply the given operation exactly twice.[BR]You have to implement an operation which takes a single-qubit operation as an input and returns an integer. The operation should have the following signature:"
// Tldr = "You should implement the empty Solve operation below and identify if the provided unitary is the H gate (return 0) or the X gate (return 1)."
// ExampleDescription = ""
// ExampleCode = ""
// ExpectedOutput = "true"
// ===EXPECTED-STATES-START===
// ===EXPECTED-STATES-END===
// CopilotInstructions = ""
// Level = 2
// ============================================================

import Std.Arithmetic.*;
import Std.Canon.*;
import Std.Diagnostics.*;
import Std.Math.*;
import Std.Measurement.*;

operation Main() : Bool
{
    // Wrap the H gate in a unitary operation to be able to check the number of times it was applied.
    let unitary1 = q => H(q);
    StartCountingOperation(unitary1);
    mutable result1Count = 0;
    for i in 1..10
    {
        let result1 = Solve(unitary1);
        set result1Count += result1 == 0 ? 1 | 0;
    }    
    LogMessage(result1Count == 10, "You have successfully identified the H-gate", "You have failed to identify the H-gate");
    let numberOfUnitary1 = StopCountingOperation(unitary1);
    LogMessage(numberOfUnitary1 == 20, "You have successfully applied the unitary operation exactly twice while identifying the H-gate", $"You have failed to apply the unitary operation exactly twice and applied it {numberOfUnitary1} times while identifying the H-gate");

    // Wrap the X gate in a unitary operation to be able to check the number of times it was applied.
    let unitary2 = q => X(q);
    StartCountingOperation(unitary2);
    mutable result2Count = 0;
    for i in 1..10
    {
        let result2 = Solve(unitary2);
        set result2Count += result2 == 1 ? 1 | 0;
    }
    LogMessage(result2Count == 10, "You have successfully identified the X-gate", "You have failed to identify the X-gate");
    let numberOfUnitary2 = StopCountingOperation(unitary2);
    LogMessage(numberOfUnitary2 == 20, "You have successfully applied the unitary operation exactly twice while identifying the X-gate", $"You have failed to apply the unitary operation exactly twice and applied it {numberOfUnitary2} times while identifying the X-gate");

    // Return true if the H gate was identified as 0 and the X gate as 1, and both unitary operations were applied exactly twice.
    return result1Count == 10 and numberOfUnitary1 == 20 and result2Count == 10 and numberOfUnitary2 == 20;
}

function LogMessage(isValid: Bool, validMessage: String, invalidMessage: String) : ()
{
    let message = "{\"valid\": " + (isValid ? "true" | "false") + ", \"message\": \"" + (isValid ? validMessage | invalidMessage) + "\"}";
    Message(message);
}

// ===SOLVE-START===
operation Solve (unitary : (Qubit => Unit)) : Int
{
    use q = Qubit();

    unitary(q);
    Z(q);
    unitary(q);

    return MResetZ(q) == Zero ? 1 | 0;
}
// ===SOLVE-END===
