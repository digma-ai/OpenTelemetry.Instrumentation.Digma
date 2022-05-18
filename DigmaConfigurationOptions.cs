// <copyright file="DigmaInstrumentationHelperExtensions.cs" company="Digma Team">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
namespace OpenTelemetry.Instrumentation.Digma
{
    public class DigmaConfigurationOptions
    {
        private const string DEFAULT_COMMIT_ENV_VAR="DEPLOYMENT_COMMIT_ID";
        private const string DEFAULT_ENV_ENV_VAR="DEPLOYMENT_ENVIORNMENT";

        public string? NamespaceRoot { get; set; } = null;
        public string? Environment { get; set; } = null;
        public string CommitIdEnvVariable { get; set; } = DEFAULT_COMMIT_ENV_VAR;
        public string EnvironmentEnvVariable { get; set; } = DEFAULT_ENV_ENV_VAR;
        public string? CommitId { get; set; } = null;
    }
}
