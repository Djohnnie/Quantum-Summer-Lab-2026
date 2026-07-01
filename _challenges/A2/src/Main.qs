// ============================================================
// CHALLENGE METADATA
// Edit the fields below directly. SolutionTemplate and
// VerificationTemplate are NOT stored here - they are derived
// automatically from the code below (see the SOLVE markers).
// After editing, run `python _challenges/generate_challenges.py`
// to regenerate QuantumSummerLab.Tools/Challenges.cs.
// ============================================================
// Name = "A2"
// Title = "Distinguish I from X"
// Description = "You are given an operation that implements a single-qubit unitary transformation: either the identity gate (I gate) or the bit-flip gate (X gate).[BR]Your task is to perform necessary operations and measurements to figure out which unitary it was and to return 0 if it was the I gate or 1 if it was the X gate.[BR]You are allowed to apply the given operation exactly once.[BR]You have to implement an operation which takes a single-qubit operation as an input and returns an integer. The operation should have the following signature:"
// Tldr = "You should implement the empty Solve operation below and identify if the provided unitary is the I gate (return 0) or the X gate (return 1)."
// ExampleDescription = ""
// ExampleCode = ""
// ExpectedOutput = "true"
// ===EXPECTED-STATES-START===
// ===EXPECTED-STATES-END===
// CopilotInstructions = ""
// Level = 1
// ============================================================

import Std.Arithmetic.*;
import Std.Canon.*;
import Std.Diagnostics.*;
import Std.Math.*;
import Std.Measurement.*;

operation Main() : Bool
{
    // Wrap the I gate in a unitary operation to be able to check the number of times it was applied.
    let unitary1 = q => I(q);
    StartCountingOperation(unitary1);
    mutable result1Count = 0;
    for i in 1..10
    {
        let result1 = Solve(unitary1);
        set result1Count += result1 == 0 ? 1 | 0;
    }
    LogMessage(result1Count == 10, "You have successfully identified the I-gate", "You have failed to identify the I-gate");
    let numberOfUnitary1 = StopCountingOperation(unitary1);
    LogMessage(numberOfUnitary1 == 10, "You have successfully applied the unitary operation exactly once while identifying the I-gate", $"You have failed to apply the unitary operation exactly once and applied it {numberOfUnitary1} times while identifying the I-gate");

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
    LogMessage(numberOfUnitary2 == 10, "You have successfully applied the unitary operation exactly once while identifying the X-gate", $"You have failed to apply the unitary operation exactly once and applied it {numberOfUnitary2} times while identifying the X-gate");

    // Return true if the I gate was identified as 0 and the X gate as 1, and both unitary operations were applied exactly once.
    return result1Count == 10 and numberOfUnitary1 == 10 and result2Count == 10 and numberOfUnitary2 == 10;
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
    return MResetZ(q) == One ? 1 | 0;
}
// ===SOLVE-END===
