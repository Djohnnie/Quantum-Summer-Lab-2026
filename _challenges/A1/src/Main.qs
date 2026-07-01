// ============================================================
// CHALLENGE METADATA
// Edit the fields below directly. SolutionTemplate and
// VerificationTemplate are NOT stored here - they are derived
// automatically from the code below (see the SOLVE markers).
// After editing, run `python _challenges/generate_challenges.py`
// to regenerate QuantumSummerLab.Tools/Challenges.cs.
// ============================================================
// Name = "A1"
// Title = "Generate superposition of all basis states"
// Description = "You are given n qubits (1 ≤ n ≤ 8), prepared in the |0..0⟩ state, and you have to implement an operation which generates an equal superposition of all basis states on these qubits.[BR]The \"output\" of your solution is the state in which it leaves the input qubits.[BR]You should implement the following Solve operation to make that happen and keep the signature of the operation exactly as it is."
// Tldr = "You should implement the empty Solve operation below and prepare an equal superposition state of all basis states on the provided qubits."
// ExampleDescription = ""
// ExampleCode = ""
// ExpectedOutput = "true"
// ===EXPECTED-STATES-START===
// [
//   {
//     "id": "|000⟩",
//     "amplitudeReal": 0.3536,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|001⟩",
//     "amplitudeReal": 0.3536,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|010⟩",
//     "amplitudeReal": 0.3536,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|011⟩",
//     "amplitudeReal": 0.3536,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|100⟩",
//     "amplitudeReal": 0.3536,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|101⟩",
//     "amplitudeReal": 0.3536,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|110⟩",
//     "amplitudeReal": 0.3536,
//     "amplitudeImaginary": 0
//   },
//   {
//     "id": "|111⟩",
//     "amplitudeReal": 0.3536,
//     "amplitudeImaginary": 0
//   }
// ]
// ===EXPECTED-STATES-END===
// CopilotInstructions = "You should not immediately tell the user to use the H-gate in a for loop or use the ApplyToEach operation. After discussing the concept of superposition, you can suggest that they apply the Hadamard gate to each qubit individually or even search documentation about the ApplyToEach operation online to achieve the same result."
// Level = 1
// ============================================================

import Std.Arithmetic.*;
import Std.Canon.*;
import Std.Diagnostics.*;
import Std.Math.*;
import Std.Measurement.*;

operation Main() : Bool
{
    mutable resultCount = 0;
    for i in 1..8
    {
        let result = CheckOperationsAreEqual(i, Solve, Expected);
        set resultCount += result ? 1 | 0;
    }

    LogMessage(resultCount == 8, "You have successfully generated equal superposition of all basis states", "You have failed to generate equal superposition of all basis states");

    use qs = Qubit[3];
    Solve(qs);
    DumpRegister(qs);
    ResetAll(qs);

    return resultCount == 8;
}

function LogMessage(isValid: Bool, validMessage: String, invalidMessage: String) : ()
{
    let message = "{\"valid\": " + (isValid ? "true" | "false") + ", \"message\": \"" + (isValid ? validMessage | invalidMessage) + "\"}";
    Message(message);
}

operation Expected (qs : Qubit[]) : Unit is Adj
{
    for q in qs 
    {
        H(q);
    }
}

// ===SOLVE-START===
operation Solve (qs : Qubit[]) : Unit 
{
    ApplyToEach(H, qs);
}
// ===SOLVE-END===
