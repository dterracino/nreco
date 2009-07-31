<xsl:stylesheet version='1.0' 
				xmlns:xsl='http://www.w3.org/1999/XSL/Transform' 
				xmlns:msxsl="urn:schemas-microsoft-com:xslt" 
				xmlns:l="urn:schemas-nreco:nreco:lucene:v1"
				xmlns:r="urn:schemas-nreco:nreco:core:v1"
				xmlns:d="urn:schemas-nreco:nicnet:dalc:v1"
				exclude-result-prefixes="msxsl l">

	<xsl:template match="l:index">
		<xsl:variable name="indexName" select="@name"/>
		<xsl:variable name="indexDir" select="@location|l:location"/>
		
		<!-- index writer factory -->
		<xsl:call-template name="component-definition">
		  <xsl:with-param name="name"><xsl:value-of select="$indexName"/>LuceneFactory</xsl:with-param>
		  <xsl:with-param name="type">NReco.Lucene.LuceneFactory,NReco.Lucene</xsl:with-param>
		  <xsl:with-param name="injections">
			<property name="IndexDir"><value><xsl:value-of select="$indexDir"/></value></property>
		  </xsl:with-param>
		</xsl:call-template> 		
		<!-- transaction manager -->
		<xsl:call-template name="component-definition">
		  <xsl:with-param name="name"><xsl:value-of select="$indexName"/>LuceneTransactionManager</xsl:with-param>
		  <xsl:with-param name="type">NReco.Lucene.TransactionManager,NReco.Lucene</xsl:with-param>
		  <xsl:with-param name="injections">
			<property name="Factories"><list><entry><ref name="{$indexName}LuceneFactory"/></entry></list></property>
		  </xsl:with-param>
		</xsl:call-template>
		
		<!-- document composers -->
		<xsl:apply-templates select="l:document" mode="lucene-document-composer">
			<xsl:with-param name="indexName" select="$indexName"/>
		</xsl:apply-templates>
		
		<!-- indexers -->
		<xsl:apply-templates select="l:indexers/l:*" mode="lucene-indexer">
			<xsl:with-param name="indexName" select="$indexName"/>
		</xsl:apply-templates>
		
		<xsl:apply-templates select="l:indexers/l:*" mode="lucene-mass-indexer">
			<xsl:with-param name="indexName" select="$indexName"/>
		</xsl:apply-templates>		
		
		<!-- reindex operation -->
		<xsl:variable name="fullReindexDsl">
			<r:operation name="{$indexName}LuceneReindexOperation">
				<r:chain>
					<!-- 1. remove old index (if exists) -->
					<r:execute>
						<r:target>
							<r:invoke method="Clear" target="{$indexName}LuceneFactory"/>
						</r:target>
					</r:execute>
					<!-- 2. run all re-indexers -->
					<xsl:for-each select="l:indexers/l:*">
						<r:execute>
							<xsl:attribute name="target"><xsl:apply-templates select="." mode="lucene-mass-indexer-name"><xsl:with-param name="indexName" select="$indexName"/></xsl:apply-templates></xsl:attribute>
						</r:execute>
					</xsl:for-each>
				</r:chain>
			</r:operation>
		</xsl:variable>
		<xsl:apply-templates select="msxsl:node-set($fullReindexDsl)/node()"/>
		
	</xsl:template>
			
	<xsl:template match="l:datarow" mode="lucene-mass-indexer-name"><xsl:param name="indexName"/><xsl:value-of select="$indexName"/>_<xsl:value-of select="@sourcename"/>_MassIndexer</xsl:template>

	<xsl:template match="l:datarow" mode="lucene-mass-indexer">
		<xsl:param name="indexName"/>
		
		<xsl:call-template name="component-definition">
		  <xsl:with-param name="name"><xsl:value-of select="$indexName"/>_<xsl:value-of select="@sourcename"/>_MassIndexer</xsl:with-param>
		  <xsl:with-param name="type">NReco.Lucene.DalcMassIndexer,NReco.Lucene</xsl:with-param>
		  <xsl:with-param name="injections">
			<property name="Dalc"><ref name="{@dalc}"/></property>
			<property name="Indexer"><ref name="{$indexName}_{@sourcename}_Indexer"/></property>
			<property name="Transaction"><ref name="{$indexName}LuceneTransactionManager"/></property>
			<property name="SourceName"><value><xsl:value-of select="@sourcename"/></value></property>
		  </xsl:with-param>
		</xsl:call-template>
	</xsl:template>
			
	<xsl:template match="l:datarow" mode="lucene-indexer">
		<xsl:param name="indexName"/>
		
		<xsl:call-template name="component-definition">
		  <xsl:with-param name="name"><xsl:value-of select="$indexName"/>_<xsl:value-of select="@sourcename"/>_Indexer</xsl:with-param>
		  <xsl:with-param name="type">NReco.Lucene.DataRowIndexer,NReco.Lucene</xsl:with-param>
		  <xsl:with-param name="injections">
			<property name="IndexWriterProvider"><ref name="{$indexName}LuceneFactory"/></property>
			<property name="DocumentProviders">
				<list>
					<xsl:for-each select="l:document">
						<entry>
							<xsl:choose>
								<xsl:when test="l:*">
									<!-- TBD - extra context -->
								</xsl:when>
								<xsl:otherwise>
									<ref name="{$indexName}_{@name}_DocumentComposer"/>
								</xsl:otherwise>
							</xsl:choose>
						</entry>
					</xsl:for-each>
				</list>
			</property>
		  </xsl:with-param>
		</xsl:call-template>		
		
	</xsl:template>

	<xsl:template match="l:document" mode="lucene-document-composer">
		<xsl:param name="indexName"/>
		<xsl:call-template name="component-definition">
		  <xsl:with-param name="name"><xsl:value-of select="$indexName"/>_<xsl:value-of select="@name"/>_DocumentComposer</xsl:with-param>
		  <xsl:with-param name="type">NReco.Lucene.DocumentComposer,NReco.Lucene</xsl:with-param>
		  <xsl:with-param name="injections">
			<property name="UidProvider">
				<xsl:call-template name="ognl-provider">
					<xsl:with-param name="name"></xsl:with-param>
					<xsl:with-param name="code"><xsl:value-of select="l:uid"/></xsl:with-param>
				</xsl:call-template>
			</property>
			<property name="Fields">
				<list>
					<xsl:for-each select="l:field">
						<entry>
							<xsl:apply-templates select="." mode="lucene-document-field"/>
						</entry>
					</xsl:for-each>
				</list>
			</property>
		  </xsl:with-param>
		</xsl:call-template>  
	</xsl:template>
    
	<xsl:template match="l:field" mode="lucene-document-field">
		<xsl:call-template name="component-definition">
		  <xsl:with-param name="name"></xsl:with-param>
		  <xsl:with-param name="type">NReco.Lucene.DocumentComposer+FieldDescriptor,NReco.Lucene</xsl:with-param>
		  <xsl:with-param name="injections">
			<property name="Name"><value><xsl:value-of select="@name"/></value></property>
			<property name="Store">
				<value>
					<xsl:choose>
						<xsl:when test="@store='0' or @store='false'">false</xsl:when>
						<xsl:otherwise>true</xsl:otherwise>
					</xsl:choose>
				</value>
			</property>
			<property name="Compress">
				<value>
					<xsl:choose>
						<xsl:when test="@compress='1' or @store='true'">true</xsl:when>
						<xsl:otherwise>false</xsl:otherwise>
					</xsl:choose>
				</value>
			</property>			
			<property name="Index">
				<value>
					<xsl:choose>
						<xsl:when test="@index='0' or @index='false'">false</xsl:when>
						<xsl:otherwise>true</xsl:otherwise>
					</xsl:choose>
				</value>
			</property>
			<property name="Analyze">
				<value>
					<xsl:choose>
						<xsl:when test="@analyze='0' or @analyze='false'">false</xsl:when>
						<xsl:otherwise>true</xsl:otherwise>
					</xsl:choose>
				</value>
			</property>
			<property name="Normalize">
				<value>
					<xsl:choose>
						<xsl:when test="@normalize='0' or @normalize='false'">false</xsl:when>
						<xsl:otherwise>true</xsl:otherwise>
					</xsl:choose>
				</value>
			</property>
			<property name="Provider">
				<xsl:call-template name="ognl-provider">
					<xsl:with-param name="name"></xsl:with-param>
					<xsl:with-param name="code"><xsl:value-of select="."/></xsl:with-param>
				</xsl:call-template>
			</property>
		  </xsl:with-param>
		</xsl:call-template>		
	</xsl:template>
	
  
</xsl:stylesheet>