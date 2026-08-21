#!/bin/bash

# Parse running .NET processes from dotnet-trace ps
processes=()
while IFS= read -r line; do
  [[ -n "$line" ]] && processes+=("$line")
done < <(dotnet-trace ps)

if [ ${#processes[@]} -eq 0 ]; then
  echo "No running .NET processes found."
  exit 1
fi

PS3="Select a .NET process to trace: "
select choice in "${processes[@]}" "Quit"; do
  case "$choice" in
    "Quit") exit 0 ;;
    "") echo "Invalid selection." ;;
    *)
      pid=$(echo "$choice" | awk '{print $1}')
      break
      ;;
  esac
done

mkdir -p ./traces/
TRACE_FILE="./traces/trace_$1_$(date +%Y-%m-%d_%H-%M-%S).nettrace"

dotnet-trace collect -p "$pid" -o "$TRACE_FILE"
dotnet-trace convert "$TRACE_FILE" --format speedscope