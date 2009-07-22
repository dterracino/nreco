<!--
NReco library (http://nreco.googlecode.com/)
Copyright 2008,2009 Vitaliy Fedorchenko
Distributed under the LGPL licence
 
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
-->	
<xsl:stylesheet version='1.0' 
				xmlns:e="urn:schemas-nreco:nreco:entity:v1"
				xmlns:l="urn:schemas-nreco:nreco:web:layout:v1"
				xmlns:xsl='http://www.w3.org/1999/XSL/Transform' 
				xmlns:msxsl="urn:schemas-microsoft-com:xslt" 
				xmlns:Plugin="urn:remove"
				xmlns:NReco="urn:remove"
				xmlns:asp="urn:remove"
				exclude-result-prefixes="msxsl">

	<xsl:output method='xml' indent='yes' />

	<xsl:template match="l:field[l:editor/l:datepicker]" mode="register-editor-control">
		@@lt;%@ Register TagPrefix="Plugin" tagName="DatePickerEditor" src="~/templates/editors/DatePickerEditor.ascx" %@@gt;
	</xsl:template>	
	
	<xsl:template match="l:field[l:editor/l:datepicker]" mode="form-view-editor">
		<Plugin:DatePickerEditor runat="server" 
			id="{@name}"
			ObjectValue='@@lt;%# Bind("{@name}") %@@gt;'/>
	</xsl:template>	
	
	<xsl:template match="l:tabs" mode="aspnet-renderer">
		<xsl:variable name="uniqueId">jqTabs<xsl:value-of select="generate-id(.)"/></xsl:variable>
		<div id="{$uniqueId}" style="display:none">
			<ul>
				<xsl:for-each select="l:tab">
					<xsl:call-template name="apply-visibility">
						<xsl:with-param name="content">
							<li><a href="#{$uniqueId}_{position()}"><xsl:value-of select="@caption"/></a></li>
						</xsl:with-param>
						<xsl:with-param name="expr" select="l:visible/node()"/>
					</xsl:call-template>
				</xsl:for-each>
			</ul>
			<xsl:for-each select="l:tab">
				<xsl:call-template name="apply-visibility">
					<xsl:with-param name="content">			
						<div id="{$uniqueId}_{position()}">
							<xsl:apply-templates select="l:renderer/node()" mode="aspnet-renderer"/>
						</div>
					</xsl:with-param>
					<xsl:with-param name="expr" select="l:visible/node()"/>
				</xsl:call-template>
			</xsl:for-each>
		</div>
		<script type="text/javascript">
			jQuery(function() {
				jQuery("#<xsl:value-of select="$uniqueId"/>").tabs().show();
			});
		</script>
	</xsl:template>
	
	<xsl:template match="l:widget" mode="aspnet-renderer">
		<xsl:variable name="uniqueId" select="generate-id(.)"/>
		<xsl:variable name="extraClass"><xsl:value-of select="name(node())"/>view</xsl:variable>
		<xsl:choose>
			<xsl:when test="@caption">
				<div id="widgetHeader{$uniqueId}" class="ui-widget-header ui-corner-top {$extraClass}">
					<div class="nreco-widget-header"><NReco:Label runat="server"><xsl:value-of select="@caption"/></NReco:Label></div>
				</div>
				<div id="widgetContent{$uniqueId}" class="ui-widget-content ui-corner-bottom {$extraClass}">
					<div class="nreco-widget-content">
					<xsl:apply-templates select="node()" mode="aspnet-renderer"/>
					</div>
				</div>
			</xsl:when>
			<xsl:otherwise>
				<div id="widgetContent{$uniqueId}" class="ui-corner-all ui-widget-content nreco-widget-content {$extraClass}">
					<xsl:apply-templates select="node()" mode="aspnet-renderer"/>
				</div>				
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	
</xsl:stylesheet>