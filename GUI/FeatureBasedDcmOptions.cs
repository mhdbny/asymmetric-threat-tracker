﻿#region copyright
// Copyright 2013-2014 The Rector & Visitors of the University of Virginia
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
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PTL.ATT.Classifiers;
using PTL.ATT.Models;

namespace PTL.ATT.GUI
{
    public partial class FeatureBasedDcmOptions : UserControl
    {
        private FeatureBasedDCM _featureBasedDCM;
        private Dictionary<string, string> _featureRemapKeyTargetPredictionResource;
        private bool _initializing;
        private Area _trainingArea;
        private Func<Area, List<Feature>> _getFeatures;

        public int TrainingPointSpacing
        {
            get { return (int)trainingPointSpacing.Value; }
        }

        public int FeatureDistanceThreshold
        {
            get { return (int)featureDistanceThreshold.Value; }
        }

        public int NegativePointStandoff
        {
            get { return (int)negativePointStandoff.Value; }
        }

        public Classifier Classifier
        {
            get { return classifiers.SelectedItem as Classifier; }
        }

        public List<Feature> Features
        {
            get { return features.SelectedItems.Cast<Feature>().ToList(); }
        }

        public Func<Area, List<Feature>> GetFeatures
        {
            set { _getFeatures = value; }
        }

        public Area TrainingArea
        {
            set
            {
                _trainingArea = value;
                RefreshAll();
            }
        }

        public FeatureBasedDCM FeatureBasedDCM
        {
            get { return _featureBasedDCM; }
            set
            {
                _featureBasedDCM = value;
                _trainingArea = null;

                if (_featureBasedDCM != null)
                {
                    _featureRemapKeyTargetPredictionResource.Clear();
                    foreach (Feature feature in _featureBasedDCM.Features)
                        if (feature.PredictionResourceId != feature.TrainingResourceId)
                            _featureRemapKeyTargetPredictionResource.Add(feature.RemapKey, feature.PredictionResourceId);

                    _trainingArea = _featureBasedDCM.TrainingArea;

                    RefreshAll();
                }
            }
        }

        public FeatureBasedDcmOptions()
        {
            _initializing = true;
            InitializeComponent();
            _initializing = false;

            _featureRemapKeyTargetPredictionResource = new Dictionary<string, string>();

            RefreshAll();
        }

        public void RefreshAll()
        {
            if (_initializing)
                return;

            trainingPointSpacing.Value = 200;
            featureDistanceThreshold.Value = 1000;
            negativePointStandoff.Value = 200;
            classifiers.Populate(_featureBasedDCM);

            RefreshFeatures();

            if (_featureBasedDCM != null)
            {
                trainingPointSpacing.Value = _featureBasedDCM.TrainingPointSpacing;
                featureDistanceThreshold.Value = _featureBasedDCM.FeatureDistanceThreshold;
                negativePointStandoff.Value = _featureBasedDCM.NegativePointStandoff;

                foreach (Feature feature in _featureBasedDCM.Features)
                {
                    int index = features.Items.IndexOf(feature);
                    Feature featureInList = features.Items[index] as Feature;
                    features.SetSelected(index, true);
                    featureInList.Parameters = feature.Parameters;
                }
            }
        }

        private void RefreshFeatures()
        {
            features.Items.Clear();

            if (_trainingArea != null)
            {
                List<Feature> availableFeatures = _getFeatures(_trainingArea);

                foreach (Feature f in availableFeatures)
                    if (_featureRemapKeyTargetPredictionResource.ContainsKey(f.RemapKey))
                        f.PredictionResourceId = _featureRemapKeyTargetPredictionResource[f.RemapKey];

                features.Items.AddRange(availableFeatures.ToArray());
            }
        }

        #region features
        public void selectAllFeaturesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < features.Items.Count; ++i)
                features.SetSelected(i, true);
        }

        private void remapSelectedFeaturesDuringPredictionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Features.Count == 0)
                MessageBox.Show("Must select one or more features before remapping.");
            else
            {
                Area[] predictionAreas = Area.GetAll().ToArray();
                if (predictionAreas.Length == 0)
                    MessageBox.Show("No prediction areas available for remapping.");
                else
                {
                    DynamicForm df = new DynamicForm("Remapping features...", DynamicForm.CloseButtons.OkCancel);
                    df.AddDropDown("Prediction area:", predictionAreas, null, "prediction_area", true);
                    if (df.ShowDialog() == DialogResult.OK)
                    {
                        List<Feature> selectedFeatures = Features;
                        FeatureRemappingForm f = new FeatureRemappingForm(selectedFeatures, _getFeatures(df.GetValue<Area>("prediction_area")));
                        f.ShowDialog();

                        _featureRemapKeyTargetPredictionResource.Clear();
                        foreach (Feature feature in selectedFeatures)
                            if (feature.PredictionResourceId != feature.TrainingResourceId)
                                _featureRemapKeyTargetPredictionResource.Add(feature.RemapKey, feature.PredictionResourceId);

                        RefreshFeatures();

                        foreach (Feature feature in selectedFeatures)
                            features.SetSelected(features.Items.IndexOf(feature), true);
                    }
                }
            }
        }
        #endregion

        public string ValidateInput()
        {
            StringBuilder errors = new StringBuilder();

            if (Classifier == null)
                errors.AppendLine("A classifier must be selected.");

            if (Features.Count == 0)
                errors.AppendLine("One or more features must be selected.");

            return errors.ToString();
        }

        private void features_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                int index = features.IndexFromPoint(e.Location);
                if (index != -1)
                {
                    Feature feature = features.Items[index] as Feature;
                    parameterizeFeatureToolStripMenuItem.Text = "Parameterize \"" + feature.Description + "\"...";
                    parameterizeFeatureToolStripMenuItem.Visible = feature.Parameters.Count > 0;
                    parameterizeFeatureToolStripMenuItem.Tag = feature;
                }

                parameterizeSelectedFeaturesToolStripMenuItem.Text = "Parameterize all " + Features.Count + " selected features...";
                parameterizeSelectedFeaturesToolStripMenuItem.Visible = Features.Count > 1;
            }
        }

        private void parameterizeFeatureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Feature feature = parameterizeFeatureToolStripMenuItem.Tag as Feature;
            DynamicForm f = new DynamicForm("Parameterize \"" + feature.Description + "\"...", DynamicForm.CloseButtons.OkCancel);
            foreach (Enum parameter in feature.Parameters.OrderBy(p => p))
                if (parameter.Equals(FeatureBasedDCM.GeometryAttributeParameter.AttributeColumn))
                    f.AddDropDown(parameter + ":", DB.Connection.GetColumnNames(new Shapefile(int.Parse(feature.TrainingResourceId)).GeometryTable).ToArray(), feature.Parameters.GetStringValue(parameter), parameter.ToString(), true, toolTipText: feature.Parameters.GetTip(parameter));
                else if (parameter.Equals(FeatureBasedDCM.GeometryAttributeParameter.AttributeType))
                    f.AddDropDown(parameter + ":", new string[] { "Numeric", "Nominal" }, feature.Parameters.GetStringValue(parameter), parameter.ToString(), true, toolTipText: feature.Parameters.GetTip(parameter));
                else
                    f.AddTextBox(parameter + ":", feature.Parameters.GetStringValue(parameter), 20, parameter.ToString(), toolTipText: feature.Parameters.GetTip(parameter));

            if (f.ShowDialog() == DialogResult.OK)
                foreach (Enum parameter in feature.Parameters.OrderBy(p => p))
                    feature.Parameters.Set(parameter, f.GetValue<string>(parameter.ToString()));
        }

        private void parameterizeSelectedFeaturesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DynamicForm f = new DynamicForm("Parameterize " + Features.Count + " features...", DynamicForm.CloseButtons.OkCancel);
            foreach (Enum parameter in Features.SelectMany(feature => feature.Parameters).Distinct().OrderBy(p => p.GetType().FullName + "." + p))
            {
                string currentValue = "";
                List<string> distinctValues = Features.Where(feature => feature.Parameters.Contains(parameter)).Select(feature => feature.Parameters.GetStringValue(parameter)).Distinct().ToList();
                if (distinctValues.Count == 1)
                    currentValue = distinctValues[0];

                string parameterId = parameter.GetType().FullName;
                parameterId = parameterId.Substring(parameterId.IndexOf('+') + 1) + "." + parameter;
                f.AddTextBox(parameterId + ":", currentValue, 20, parameterId, toolTipText: Features.Where(feature => feature.Parameters.Contains(parameter)).First().Parameters.GetTip(parameter));
            }

            if (f.ShowDialog() == DialogResult.OK)
                foreach (string parameterId in f.ValueIds)
                {
                    string[] enumTypeEnumValue = parameterId.Split('.');
                    string enumType = enumTypeEnumValue[0];
                    string enumValue = enumTypeEnumValue[1];
                    foreach (Feature feature in Features)
                        foreach (Enum parameter in feature.Parameters.ToArray())
                            if (parameter.GetType().FullName.EndsWith("+" + enumType) && parameter.ToString() == enumValue)
                                feature.Parameters.Set(parameter, f.GetValue<string>(parameterId));
                }
        }

        internal void CommitValues(FeatureBasedDCM model)
        {
            model.TrainingPointSpacing = TrainingPointSpacing;
            model.FeatureDistanceThreshold = FeatureDistanceThreshold;
            model.NegativePointStandoff = NegativePointStandoff;
            model.Classifier = Classifier;
            model.Features = Features;
        }
    }
}
